Below is a complete `README.md` suitable for the root of the **XliffBatchTranslate** repository. It is written to be clear to engineers, product owners, and reviewers evaluating the tool as part of a localization or cost-reduction workflow.

---

# XliffBatchTranslate

**XliffBatchTranslate** is a .NET 8 console application that performs **batch, first-pass translation of XLIFF files** using a locally hosted Large Language Model (LLM), such as one served by **LM Studio**.
It is designed to automate the most time-consuming portion of enterprise UI localization while preserving XLIFF structure, placeholders, and formatting.

The intended usage model is **LLM first-pass translation → human review and validation**, reducing overall translation cost and turnaround time for enterprise and medical software.

---

## Why This Exists

Enterprise medical software localization is expensive and slow when every release requires full human translation. Typical outsourced medical translation costs range from **$0.20–$0.35 per word**, plus additional QA and UI validation costs, often pushing a single-language release into the **$10k–$20k** range for ~50,000 words .

This tool shifts that cost model by:

* Using an **LLM to generate an initial translation**
* Preserving **XLIFF correctness and placeholders**
* Allowing vendors to focus on **verification and correction**, not raw translation
* Supporting **continuous localization** workflows with partial updates

---

## Key Features

* **Batch XLIFF processing**

    * Reads one or more `.xlf` / `.xliff` files
    * Writes translated `<target>` elements in place

* **LLM-backed translation**

    * Uses a Chat Completion–style HTTP API
    * Tested with LM Studio local models
    * Model-agnostic (any compatible API can be used)

* **Placeholder protection**

    * Preserves tokens such as `{0}`, `%s`, XML tags, and ICU-style placeholders
    * Prevents accidental corruption of runtime strings

* **Token-aware batching**

    * Splits translation requests to stay within model token limits
    * Optimized for large enterprise string sets

* **Deterministic, automatable output**

    * Safe to run in CI or build pipelines
    * Produces vendor-ready XLIFF files

---

## What This Tool Is *Not*

* ❌ A replacement for professional medical translation
* ❌ A linguistic QA or regulatory validation tool
* ❌ A runtime localization framework

This tool intentionally **does not** attempt to solve final linguistic correctness. It exists to **reduce cost and effort**, not remove human oversight.

---

## Architecture Overview

```
┌─────────────────────┐
│ XLIFF Files (en)    │
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│ XliffTranslator     │
│  • Parses XLIFF     │
│  • Protects tokens  │
│  • Batches strings  │
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│ LMStudioClient      │
│  • HTTP API calls   │
│  • Chat completions │
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│ Translated XLIFF    │
│ (<target> filled)  │
└─────────────────────┘
```

---

## Requirements

* **.NET 8 SDK**
* **LM Studio** (or compatible LLM API)
* A translation-capable model (e.g. English → Spanish)

---

## Example Usage

```bash
dotnet run -- \
  --input ./xliff/en-US \
  --output ./xliff/es-ES \
  --source-lang en \
  --target-lang es \
  --endpoint http://localhost:1234/v1/chat/completions \
  --model towerinstruct-7b-v0.2-en2es
```

> Exact CLI flags may evolve; see `Program.cs` for authoritative argument handling.

---

## Supported Use Cases

* Hack-a-thons and internal demos
* Cost-reduction proofs of concept
* Continuous localization pipelines
* Pre-translation before vendor handoff
* Rapid iteration on UI text changes

---

## Recommended Workflow

1. **Extract XLIFF from the application**
2. **Run XliffBatchTranslate** for first-pass translation
3. **Hand off translated XLIFF to a vendor**
4. Vendor performs:

    * Linguistic correction
    * Terminology validation
    * In-context UI QA
5. **Re-import verified XLIFF**

This mirrors modern enterprise localization best practices while reducing spend .

---

## Design Principles

* **Safety over creativity**
  Deterministic output is preferred to stylistic variation.

* **Structure preservation first**
  XLIFF validity is never compromised.

* **Vendor-friendly output**
  Files remain compatible with CAT tools and TMS platforms.

* **Medical-domain awareness**
  Prompts and handling assume clinical / diagnostic software context.

---

## Future Enhancements (Non-Goals Today)

* Translation memory integration
* Glossary enforcement
* RTL layout validation
* In-context UI rendering
* Automated linguistic QA scoring

These are intentionally out of scope for the current tool.

---

## License

Internal / experimental use.
Review licensing implications of chosen LLM models separately.

---

## Key Takeaway

**XliffBatchTranslate** converts translation from a *full-cost service* into a *review-focused activity*, enabling faster releases and significant cost reduction while maintaining professional translation standards where they matter most.
