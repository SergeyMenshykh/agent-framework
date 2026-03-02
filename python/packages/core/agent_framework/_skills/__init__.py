# Copyright (c) Microsoft. All rights reserved.

"""Agent Skills package.

.. warning:: Experimental

    The agent skills API is experimental and subject to change or removal
    in future versions without notice.

Exports the public API for defining, discovering, and exposing agent skills:

- :class:`AgentSkill` — a skill definition with optional dynamic resources.
- :class:`AgentSkillResource` — a static or callable resource attached to a skill.
- :class:`AgentSkillsProvider` — a context provider that advertises skills to the model
  and exposes ``load_skill`` / ``read_skill_resource`` tools.
"""

from ._agent_skills_provider import AgentSkillsProvider
from ._models import AgentSkill, AgentSkillResource

__all__ = [
    "AgentSkill",
    "AgentSkillResource",
    "AgentSkillsProvider",
]
