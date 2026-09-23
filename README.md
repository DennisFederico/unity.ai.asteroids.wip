# unity.ai.sandbox

Base project with assets to test Agentic development with unity

## OKF Initialization

```bash
# Linux
git submodule add git@github.com:DennisFederico/unity.ecs.okf.knowledge.git .knowledge
```

## Skill Initialization

[Skills Repo](https://github.com/DennisFederico/ai.skills.collection)

```bash
git submodule add git@github.com:DennisFederico/ai.skills.collection.git .agents/skills

cd .agents/skills

git sparse-checkout init --cone
git sparse-checkout set okf-curator unity-ecs-patterns unity-error-resolution unity-knowledge-lookup
```

## Bootstrap for Zoocode

This is another [submodule](https://github.com/DennisFederico/unity.ai.zoocode.bootstrap), files must be copy/paste in the appropiate directories

```bash
git submodule add git@github.com:DennisFederico/unity.ai.zoocode.bootstrap.git .ai-bootstrap
```

## Install Anklebreaker MCP Bridge

Instructions are in the bootstrap above
