# Copyright (c) Microsoft. All rights reserved.

"""Tests for file-based Agent Skills provider."""

from __future__ import annotations

from pathlib import Path
from unittest.mock import AsyncMock

import pytest

from agent_framework import FileAgentSkillsProvider, SessionContext
from agent_framework._skills import (
    _build_skills_instruction_prompt,
    _discover_and_load_skills,
    _extract_resource_paths,
    _FileAgentSkill,
    _normalize_resource_path,
    _read_skill_resource,
    _SkillFrontmatter,
    _try_parse_skill_document,
)

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _write_skill(
    base: Path,
    name: str,
    description: str = "A test skill.",
    body: str = "# Instructions\nDo the thing.",
    *,
    extra_frontmatter: str = "",
    resources: dict[str, str] | None = None,
) -> Path:
    """Create a skill directory with SKILL.md and optional resource files."""
    skill_dir = base / name
    skill_dir.mkdir(parents=True, exist_ok=True)

    frontmatter = f"---\nname: {name}\ndescription: {description}\n{extra_frontmatter}---\n"
    skill_md = skill_dir / "SKILL.md"
    skill_md.write_text(frontmatter + body, encoding="utf-8")

    if resources:
        for rel_path, content in resources.items():
            res_file = skill_dir / rel_path
            res_file.parent.mkdir(parents=True, exist_ok=True)
            res_file.write_text(content, encoding="utf-8")

    return skill_dir


# ---------------------------------------------------------------------------
# Tests: module-level helper functions
# ---------------------------------------------------------------------------


class TestNormalizeResourcePath:
    """Tests for _normalize_resource_path."""

    def test_strips_dot_slash_prefix(self) -> None:
        assert _normalize_resource_path("./refs/doc.md") == "refs/doc.md"

    def test_replaces_backslashes(self) -> None:
        assert _normalize_resource_path("refs\\doc.md") == "refs/doc.md"

    def test_strips_dot_slash_and_replaces_backslashes(self) -> None:
        assert _normalize_resource_path(".\\refs\\doc.md") == "refs/doc.md"

    def test_no_change_for_clean_path(self) -> None:
        assert _normalize_resource_path("refs/doc.md") == "refs/doc.md"


class TestExtractResourcePaths:
    """Tests for _extract_resource_paths."""

    def test_extracts_markdown_links(self) -> None:
        content = "See [doc](refs/FAQ.md) and [template](assets/template.md)."
        paths = _extract_resource_paths(content)
        assert paths == ["refs/FAQ.md", "assets/template.md"]

    def test_deduplicates_case_insensitive(self) -> None:
        content = "See [a](refs/FAQ.md) and [b](refs/faq.md)."
        paths = _extract_resource_paths(content)
        assert len(paths) == 1

    def test_normalizes_dot_slash_prefix(self) -> None:
        content = "See [doc](./refs/FAQ.md)."
        paths = _extract_resource_paths(content)
        assert paths == ["refs/FAQ.md"]

    def test_ignores_urls(self) -> None:
        content = "See [link](https://example.com/doc.md)."
        paths = _extract_resource_paths(content)
        assert paths == []

    def test_empty_content(self) -> None:
        assert _extract_resource_paths("") == []

    def test_extracts_backtick_quoted_paths(self) -> None:
        content = "Use the template at `assets/template.md` and the script `./scripts/run.py`."
        paths = _extract_resource_paths(content)
        assert paths == ["assets/template.md", "scripts/run.py"]

    def test_deduplicates_across_link_and_backtick(self) -> None:
        content = "See [doc](refs/FAQ.md) and also `refs/FAQ.md`."
        paths = _extract_resource_paths(content)
        assert len(paths) == 1


class TestTryParseSkillDocument:
    """Tests for _try_parse_skill_document."""

    def test_valid_skill(self) -> None:
        content = "---\nname: test-skill\ndescription: A test skill.\n---\n# Body\nInstructions here."
        result = _try_parse_skill_document(content, "test.md")
        assert result is not None
        frontmatter, body = result
        assert frontmatter.name == "test-skill"
        assert frontmatter.description == "A test skill."
        assert "Instructions here." in body

    def test_quoted_values(self) -> None:
        content = "---\nname: \"test-skill\"\ndescription: 'A test skill.'\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is not None
        assert result[0].name == "test-skill"
        assert result[0].description == "A test skill."

    def test_utf8_bom(self) -> None:
        content = "\ufeff---\nname: test-skill\ndescription: A test skill.\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is not None
        assert result[0].name == "test-skill"

    def test_missing_frontmatter(self) -> None:
        content = "# Just a markdown file\nNo frontmatter here."
        result = _try_parse_skill_document(content, "test.md")
        assert result is None

    def test_missing_name(self) -> None:
        content = "---\ndescription: A test skill.\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is None

    def test_missing_description(self) -> None:
        content = "---\nname: test-skill\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is None

    def test_invalid_name_uppercase(self) -> None:
        content = "---\nname: Test-Skill\ndescription: A test skill.\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is None

    def test_invalid_name_starts_with_hyphen(self) -> None:
        content = "---\nname: -test-skill\ndescription: A test skill.\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is None

    def test_invalid_name_ends_with_hyphen(self) -> None:
        content = "---\nname: test-skill-\ndescription: A test skill.\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is None

    def test_name_too_long(self) -> None:
        long_name = "a" * 65
        content = f"---\nname: {long_name}\ndescription: A test skill.\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is None

    def test_description_too_long(self) -> None:
        long_desc = "a" * 1025
        content = f"---\nname: test-skill\ndescription: {long_desc}\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is None

    def test_extra_metadata_ignored(self) -> None:
        content = "---\nname: test-skill\ndescription: A test skill.\nauthor: someone\nversion: 1.0\n---\nBody."
        result = _try_parse_skill_document(content, "test.md")
        assert result is not None
        assert result[0].name == "test-skill"


# ---------------------------------------------------------------------------
# Tests: skill discovery and loading
# ---------------------------------------------------------------------------


class TestDiscoverAndLoadSkills:
    """Tests for _discover_and_load_skills."""

    def test_discovers_valid_skill(self, tmp_path: Path) -> None:
        _write_skill(tmp_path, "my-skill")
        skills = _discover_and_load_skills([str(tmp_path)])
        assert "my-skill" in skills
        assert skills["my-skill"].frontmatter.name == "my-skill"

    def test_discovers_nested_skills(self, tmp_path: Path) -> None:
        skills_dir = tmp_path / "skills"
        _write_skill(skills_dir, "skill-a")
        _write_skill(skills_dir, "skill-b")
        skills = _discover_and_load_skills([str(skills_dir)])
        assert len(skills) == 2
        assert "skill-a" in skills
        assert "skill-b" in skills

    def test_skips_invalid_skill(self, tmp_path: Path) -> None:
        skill_dir = tmp_path / "bad-skill"
        skill_dir.mkdir()
        (skill_dir / "SKILL.md").write_text("No frontmatter here.", encoding="utf-8")
        skills = _discover_and_load_skills([str(tmp_path)])
        assert len(skills) == 0

    def test_deduplicates_skill_names(self, tmp_path: Path) -> None:
        dir1 = tmp_path / "dir1"
        dir2 = tmp_path / "dir2"
        _write_skill(dir1, "my-skill", body="First")
        _write_skill(dir2, "my-skill", body="Second")
        skills = _discover_and_load_skills([str(dir1), str(dir2)])
        assert len(skills) == 1
        assert skills["my-skill"].body == "First"

    def test_empty_directory(self, tmp_path: Path) -> None:
        skills = _discover_and_load_skills([str(tmp_path)])
        assert len(skills) == 0

    def test_nonexistent_directory(self) -> None:
        skills = _discover_and_load_skills(["/nonexistent/path"])
        assert len(skills) == 0

    def test_multiple_paths(self, tmp_path: Path) -> None:
        dir1 = tmp_path / "dir1"
        dir2 = tmp_path / "dir2"
        _write_skill(dir1, "skill-a")
        _write_skill(dir2, "skill-b")
        skills = _discover_and_load_skills([str(dir1), str(dir2)])
        assert len(skills) == 2

    def test_depth_limit(self, tmp_path: Path) -> None:
        # Depth 0: tmp_path itself
        # Depth 1: tmp_path/level1
        # Depth 2: tmp_path/level1/level2 (should be found)
        # Depth 3: tmp_path/level1/level2/level3 (should NOT be found)
        deep = tmp_path / "level1" / "level2" / "level3"
        deep.mkdir(parents=True)
        (deep / "SKILL.md").write_text("---\nname: deep-skill\ndescription: Too deep.\n---\nBody.", encoding="utf-8")
        skills = _discover_and_load_skills([str(tmp_path)])
        assert "deep-skill" not in skills

    def test_skill_with_resources(self, tmp_path: Path) -> None:
        _write_skill(
            tmp_path,
            "my-skill",
            body="See [doc](refs/FAQ.md).",
            resources={"refs/FAQ.md": "FAQ content"},
        )
        skills = _discover_and_load_skills([str(tmp_path)])
        assert "my-skill" in skills
        assert skills["my-skill"].resource_names == ["refs/FAQ.md"]

    def test_excludes_skill_with_missing_resource(self, tmp_path: Path) -> None:
        _write_skill(
            tmp_path,
            "my-skill",
            body="See [doc](refs/MISSING.md).",
        )
        skills = _discover_and_load_skills([str(tmp_path)])
        assert len(skills) == 0

    def test_excludes_skill_with_path_traversal_resource(self, tmp_path: Path) -> None:
        _write_skill(
            tmp_path,
            "my-skill",
            body="See [doc](../secret.md).",
            resources={},  # resource points outside
        )
        # Create the file outside the skill directory
        (tmp_path / "secret.md").write_text("secret", encoding="utf-8")
        skills = _discover_and_load_skills([str(tmp_path)])
        assert len(skills) == 0


# ---------------------------------------------------------------------------
# Tests: read_skill_resource
# ---------------------------------------------------------------------------


class TestReadSkillResource:
    """Tests for _read_skill_resource."""

    def test_reads_valid_resource(self, tmp_path: Path) -> None:
        _write_skill(
            tmp_path,
            "my-skill",
            body="See [doc](refs/FAQ.md).",
            resources={"refs/FAQ.md": "FAQ content here"},
        )
        skills = _discover_and_load_skills([str(tmp_path)])
        content = _read_skill_resource(skills["my-skill"], "refs/FAQ.md")
        assert content == "FAQ content here"

    def test_normalizes_dot_slash(self, tmp_path: Path) -> None:
        _write_skill(
            tmp_path,
            "my-skill",
            body="See [doc](refs/FAQ.md).",
            resources={"refs/FAQ.md": "FAQ content"},
        )
        skills = _discover_and_load_skills([str(tmp_path)])
        content = _read_skill_resource(skills["my-skill"], "./refs/FAQ.md")
        assert content == "FAQ content"

    def test_unregistered_resource_raises(self, tmp_path: Path) -> None:
        _write_skill(tmp_path, "my-skill")
        skills = _discover_and_load_skills([str(tmp_path)])
        with pytest.raises(ValueError, match="not found in skill"):
            _read_skill_resource(skills["my-skill"], "nonexistent.md")

    def test_case_insensitive_lookup_uses_registered_casing(self, tmp_path: Path) -> None:
        _write_skill(
            tmp_path,
            "my-skill",
            body="See [doc](refs/FAQ.md).",
            resources={"refs/FAQ.md": "FAQ content"},
        )
        skills = _discover_and_load_skills([str(tmp_path)])
        # Request with different casing; the registered name should be used for the file path
        content = _read_skill_resource(skills["my-skill"], "REFS/faq.md")
        assert content == "FAQ content"

    def test_path_traversal_raises(self, tmp_path: Path) -> None:
        skill = _FileAgentSkill(
            frontmatter=_SkillFrontmatter("test", "Test skill"),
            body="Body",
            source_path=str(tmp_path / "skill"),
            resource_names=["../secret.md"],
        )
        (tmp_path / "secret.md").write_text("secret", encoding="utf-8")
        with pytest.raises(ValueError, match="outside the skill directory"):
            _read_skill_resource(skill, "../secret.md")

    def test_similar_prefix_directory_does_not_match(self, tmp_path: Path) -> None:
        """A skill directory named 'skill-a-evil' must not access resources from 'skill-a'."""
        skill = _FileAgentSkill(
            frontmatter=_SkillFrontmatter("test", "Test skill"),
            body="Body",
            source_path=str(tmp_path / "skill-a"),
            resource_names=["../skill-a-evil/secret.md"],
        )
        evil_dir = tmp_path / "skill-a-evil"
        evil_dir.mkdir()
        (evil_dir / "secret.md").write_text("evil", encoding="utf-8")
        with pytest.raises(ValueError, match="outside the skill directory"):
            _read_skill_resource(skill, "../skill-a-evil/secret.md")


# ---------------------------------------------------------------------------
# Tests: _build_skills_instruction_prompt
# ---------------------------------------------------------------------------


class TestBuildSkillsInstructionPrompt:
    """Tests for _build_skills_instruction_prompt."""

    def test_returns_none_for_empty_skills(self) -> None:
        assert _build_skills_instruction_prompt(None, {}) is None

    def test_default_prompt_contains_skills(self) -> None:
        skills = {
            "my-skill": _FileAgentSkill(
                frontmatter=_SkillFrontmatter("my-skill", "Does stuff."),
                body="Body",
                source_path="/tmp/skill",
            ),
        }
        prompt = _build_skills_instruction_prompt(None, skills)
        assert prompt is not None
        assert "<name>my-skill</name>" in prompt
        assert "<description>Does stuff.</description>" in prompt
        assert "load_skill" in prompt

    def test_skills_sorted_alphabetically(self) -> None:
        skills = {
            "zebra": _FileAgentSkill(
                frontmatter=_SkillFrontmatter("zebra", "Z skill."),
                body="Body",
                source_path="/tmp/z",
            ),
            "alpha": _FileAgentSkill(
                frontmatter=_SkillFrontmatter("alpha", "A skill."),
                body="Body",
                source_path="/tmp/a",
            ),
        }
        prompt = _build_skills_instruction_prompt(None, skills)
        assert prompt is not None
        alpha_pos = prompt.index("alpha")
        zebra_pos = prompt.index("zebra")
        assert alpha_pos < zebra_pos

    def test_xml_escapes_metadata(self) -> None:
        skills = {
            "my-skill": _FileAgentSkill(
                frontmatter=_SkillFrontmatter("my-skill", 'Uses <tags> & "quotes"'),
                body="Body",
                source_path="/tmp/skill",
            ),
        }
        prompt = _build_skills_instruction_prompt(None, skills)
        assert prompt is not None
        assert "&lt;tags&gt;" in prompt
        assert "&amp;" in prompt

    def test_custom_prompt_template(self) -> None:
        skills = {
            "my-skill": _FileAgentSkill(
                frontmatter=_SkillFrontmatter("my-skill", "Does stuff."),
                body="Body",
                source_path="/tmp/skill",
            ),
        }
        custom = "Custom header:\n{0}\nCustom footer."
        prompt = _build_skills_instruction_prompt(custom, skills)
        assert prompt is not None
        assert prompt.startswith("Custom header:")
        assert prompt.endswith("Custom footer.")

    def test_invalid_prompt_template_raises(self) -> None:
        with pytest.raises(ValueError, match="valid format string"):
            _build_skills_instruction_prompt("{invalid}", {})


# ---------------------------------------------------------------------------
# Tests: FileAgentSkillsProvider
# ---------------------------------------------------------------------------


class TestFileAgentSkillsProvider:
    """Tests for the public FileAgentSkillsProvider class."""

    def test_default_source_id(self, tmp_path: Path) -> None:
        provider = FileAgentSkillsProvider(str(tmp_path))
        assert provider.source_id == "file_agent_skills"

    def test_custom_source_id(self, tmp_path: Path) -> None:
        provider = FileAgentSkillsProvider(str(tmp_path), source_id="custom")
        assert provider.source_id == "custom"

    def test_accepts_single_path_string(self, tmp_path: Path) -> None:
        _write_skill(tmp_path, "my-skill")
        provider = FileAgentSkillsProvider(str(tmp_path))
        assert len(provider._skills) == 1

    def test_accepts_sequence_of_paths(self, tmp_path: Path) -> None:
        dir1 = tmp_path / "dir1"
        dir2 = tmp_path / "dir2"
        _write_skill(dir1, "skill-a")
        _write_skill(dir2, "skill-b")
        provider = FileAgentSkillsProvider([str(dir1), str(dir2)])
        assert len(provider._skills) == 2

    async def test_before_run_with_skills(self, tmp_path: Path) -> None:
        _write_skill(tmp_path, "my-skill")
        provider = FileAgentSkillsProvider(str(tmp_path))
        context = SessionContext(input_messages=[])

        await provider.before_run(
            agent=AsyncMock(),
            session=AsyncMock(),
            context=context,
            state={},
        )

        assert len(context.instructions) == 1
        assert "my-skill" in context.instructions[0]
        assert len(context.tools) == 2
        tool_names = {t.name for t in context.tools}
        assert tool_names == {"load_skill", "read_skill_resource"}

    async def test_before_run_without_skills(self, tmp_path: Path) -> None:
        provider = FileAgentSkillsProvider(str(tmp_path))
        context = SessionContext(input_messages=[])

        await provider.before_run(
            agent=AsyncMock(),
            session=AsyncMock(),
            context=context,
            state={},
        )

        assert len(context.instructions) == 0
        assert len(context.tools) == 0

    def test_load_skill_returns_body(self, tmp_path: Path) -> None:
        _write_skill(tmp_path, "my-skill", body="Skill body content.")
        provider = FileAgentSkillsProvider(str(tmp_path))
        result = provider._load_skill("my-skill")
        assert result == "Skill body content."

    def test_load_skill_unknown_returns_error(self, tmp_path: Path) -> None:
        provider = FileAgentSkillsProvider(str(tmp_path))
        result = provider._load_skill("nonexistent")
        assert result.startswith("Error:")

    def test_load_skill_empty_name_returns_error(self, tmp_path: Path) -> None:
        provider = FileAgentSkillsProvider(str(tmp_path))
        result = provider._load_skill("")
        assert result.startswith("Error:")

    def test_read_skill_resource_returns_content(self, tmp_path: Path) -> None:
        _write_skill(
            tmp_path,
            "my-skill",
            body="See [doc](refs/FAQ.md).",
            resources={"refs/FAQ.md": "FAQ content"},
        )
        provider = FileAgentSkillsProvider(str(tmp_path))
        result = provider._read_skill_resource("my-skill", "refs/FAQ.md")
        assert result == "FAQ content"

    def test_read_skill_resource_unknown_skill_returns_error(self, tmp_path: Path) -> None:
        provider = FileAgentSkillsProvider(str(tmp_path))
        result = provider._read_skill_resource("nonexistent", "file.md")
        assert result.startswith("Error:")

    def test_read_skill_resource_empty_name_returns_error(self, tmp_path: Path) -> None:
        _write_skill(tmp_path, "my-skill")
        provider = FileAgentSkillsProvider(str(tmp_path))
        result = provider._read_skill_resource("my-skill", "")
        assert result.startswith("Error:")

    def test_read_skill_resource_unknown_resource_returns_error(self, tmp_path: Path) -> None:
        _write_skill(tmp_path, "my-skill")
        provider = FileAgentSkillsProvider(str(tmp_path))
        result = provider._read_skill_resource("my-skill", "nonexistent.md")
        assert result.startswith("Error:")

    async def test_skills_sorted_in_prompt(self, tmp_path: Path) -> None:
        skills_dir = tmp_path / "skills"
        _write_skill(skills_dir, "zebra", description="Z skill.")
        _write_skill(skills_dir, "alpha", description="A skill.")
        provider = FileAgentSkillsProvider(str(skills_dir))
        context = SessionContext(input_messages=[])

        await provider.before_run(
            agent=AsyncMock(),
            session=AsyncMock(),
            context=context,
            state={},
        )

        prompt = context.instructions[0]
        assert prompt.index("alpha") < prompt.index("zebra")

    async def test_xml_escaping_in_prompt(self, tmp_path: Path) -> None:
        _write_skill(tmp_path, "my-skill", description="Uses <tags> & stuff")
        provider = FileAgentSkillsProvider(str(tmp_path))
        context = SessionContext(input_messages=[])

        await provider.before_run(
            agent=AsyncMock(),
            session=AsyncMock(),
            context=context,
            state={},
        )

        prompt = context.instructions[0]
        assert "&lt;tags&gt;" in prompt
        assert "&amp;" in prompt
