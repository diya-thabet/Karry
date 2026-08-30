# Docs Structure & Placement Guide (for new LLMs/agents)

> **Read this first.** This explains how the `docs/` folder is organized and — most importantly — **where to put any new `.md` file you generate or update**. Following this keeps the doc base searchable and consistent.

The `docs/` folder was reorganized from a flat pile of files into a **layer-based, numbered structure**. Every doc lives in exactly one place, chosen by *what it is about*, not by who wrote it.

## The layers

```
docs/
├── README.md                     # master index — list every doc here
├── 00-guidelines/                # guides like this one (how to work)
├── 01-planning/                  # roadmap, phased execution plans
├── 02-reference/                 # canonical/unambiguous specs (codex.tex)
├── 03-backend/                   # .NET backend — architecture, domain, API, DB
├── 04-frontend/                  # React PWA
├── 05-math-engine/               # Python FastAPI math service
├── 06-infrastructure/            # docker, CI/CD, tooling, local dev
├── 07-execution/                 # phase logs (what was actually done per phase)
├── 08-knowledge/                 # llmhandsoff.md — lessons learned, gotchas
└── architecture.md               # cross-cutting system overview (all layers)
```

## Where does a new doc go? (decision table)

| The doc is about… | Put it in |
|---|---|
| A phased plan / roadmap / future work | `01-planning/` (e.g. `IMPLEMENTATION_PLAN.md`) |
| A canonical spec that code must match | `02-reference/` (e.g. `codex.tex`) |
| Backend structure, entities, endpoints, DB schema | `03-backend/` |
| Frontend structure, components, PWA behavior | `04-frontend/` |
| Math formulas, Python service contracts | `05-math-engine/` |
| Compose, Dockerfiles, CI workflows, env vars | `06-infrastructure/` |
| Summary of work completed in a phase | `07-execution/phaseN-<slug>.md` |
| A problem solved / non-obvious fix / gotcha | `08-knowledge/llmhandsoff.md` (entry at bottom) |
| Something spanning multiple layers | `architecture.md` (top level) |
| This type of meta-guide | `00-guidelines/` |

## Hard rules

1. **Never create a doc at the `docs/` root** unless it is a cross-layer overview (`architecture.md` style) or the master index. Everything else goes in a numbered subfolder.
2. **Never add a new top-level folder** for a one-off doc — reuse the existing layers. If you think you need a new layer, say so in your summary instead of doing it silently.
3. **Every phase you complete** → append a log at `07-execution/phaseN-<slug>.md` and link it from `docs/README.md`.
4. **Every non-obvious fix/decision** → append an entry to `08-knowledge/llmhandsoff.md` (newest at **bottom**). Keep it: symptom → root cause → fix.
5. **Architecture changes** → update the relevant layer's `0N-architecture.md` (and `architecture.md` if it spans layers) **and** add a `llmhandsoff.md` entry.
6. **Keep the index fresh** — whenever you add or move a doc, update the search-by-subject table in `docs/README.md`.
7. **Links are relative** from the doc's own location (e.g. `../02-reference/codex.tex` from `03-backend/`). Verify any link you write resolves — a broken link check is: from the doc's folder, the target path must exist.

## Naming

- Folders: `NN-layer-name/` (two-digit sort prefix).
- Files within a layer: `NN-short-name.md` (two-digit number keeps order meaningful, e.g. `01-architecture.md`, `02-domain.md`).
- Phase logs: `phaseN-<slug>.md`.

This structure is deliberately flat per layer and consistent across layers, so a new LLM can guess where a doc lives without reading everything.