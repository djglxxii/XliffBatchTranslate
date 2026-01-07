### Key Recommendations for Prompts with Unbabel/TowerInstruct-7B-v0.2

- Research indicates that the model performs optimally without a system prompt, as it was fine-tuned using ChatML templates focused on direct user instructions.
- For user prompts, employ a structured format starting with a clear task directive, followed by the source language/text and a delimiter like "\n{target_lang}:" to cue the output, enhancing accuracy across supported languages.
- Evidence suggests incorporating context (e.g., previous sentences), terminology glossaries, or few-shot examples for complex tasks like context-aware translation or post-editing, though zero-shot prompts suffice for general use and may reduce hallucinations.
- It seems likely that multi-turn prompts support iterative refinements, such as follow-up translations or error corrections, leveraging the model's multi-turn fine-tuning on datasets like UltraChat.

#### Optimal System Prompts
TowerInstruct-7B-v0.2 does not require or use system prompts; its training on ChatML emphasizes standalone user messages. If needed for consistency in custom setups, a minimal one like "You are a multilingual translation assistant" could be tested, but this may interfere with fine-tuning and is not recommended.

#### Top User Prompt Examples
- **Basic Translation**: "Translate the following text from Portuguese into English.\nPortuguese: {text}\nEnglish:"
- **Context-Aware**: "Translate this sentence from English to German, considering the dialogue history: {history}\nEnglish: {text}\nGerman:"
- **Terminology-Aware**: "Translate from English to Spanish using these terms: 'avalanche beacon' -> 'dispositivo de búsqueda en avalanchas'.\nEnglish: {text}\nSpanish:"
- **Post-Editing**: "Post-edit this machine translation from English to German, making minimal changes to correct errors.\nEnglish: {source}\nMT: {mt}\nPost-edited:"

#### Best Practices
Always apply the tokenizer's chat template via `apply_chat_template` for formatting. Use zero-shot for simplicity, adding 1-5 examples for tasks like grammatical error correction. Limit input length to avoid inconsistencies, and specify languages/dialects for nuance.

---

Unbabel/TowerInstruct-7B-v0.2 is a 7-billion-parameter multilingual LLM fine-tuned from TowerBase-7B-v0.1 on the TowerBlocks-v0.1 dataset, optimized for translation-related tasks across 10 languages: English, Portuguese, Spanish, French, German, Dutch, Italian, Korean, Chinese, and Russian. It excels in sentence-level and paragraph-level machine translation, terminology-aware and context-aware translation, automatic post-editing (APE), named entity recognition (NER), grammatical error correction, paraphrase generation, and MT evaluation, often rivaling larger models like GPT-4 in benchmarks such as FLORES-200 and WMT23. The model's training incorporates diverse templates from TowerBlocks, which includes zero-shot (75% of records) and few-shot (1, 3, or 5 examples) instructions reformulated as natural language prompts, drawing from high-quality datasets filtered for accuracy (e.g., X_COMET-QE ≥ 0.85).

Prompt engineering for this model relies on the ChatML format without system prompts, where user instructions are wrapped in `<|im_start|>user\n{prompt}<|im_end|>\n<|im_start|>assistant`, enabling clean multi-turn interactions. This structure, extended with custom tokens for delimiters, supports deterministic outputs via greedy decoding or beam search (size 5), with Minimum Bayes Risk (MBR) for enhanced quality in evaluations. While TowerBlocks emphasizes NER with BIO tagging prompts (e.g., providing entity taxonomies like Person, Location, Group), translation tasks use directive phrasing to specify source/target languages, text, and cues for output isolation, reducing ambiguity. Limitations include potential hallucinations outside supported languages/tasks and suboptimal document-level translation, recommending short inputs and verification.

For translation, zero-shot prompts dominate, but few-shot variants improve tasks like GEC; multi-turn setups allow refinements, as seen in dialog contexts with history integration via triples (subject-predicate-object with annotations for sentiment, polarity, certainty, dialogue act). In deployment (e.g., via Transformers pipeline with bfloat16 and auto device mapping), format messages with `tokenizer.apply_chat_template` for optimal inference.

To illustrate, the following table categorizes user prompt examples from model documentation, papers, and related implementations:

| Category | Prompt Example | Use Case | Rationale |
|----------|----------------|----------|-----------|
| Basic Translation | "Translate the following text from Portuguese into English.\nPortuguese: Ontem, a minha amiga foi ao supermercado mas estava fechado. Queria comprar legumes e fruta.\nEnglish:" | General sentence-level MT | Mirrors fine-tuning templates; delimiter ensures focused output. |
| Multi-Turn Translation | "Can you now translate it into Spanish?" (following prior English output) | Iterative refinements in dialogs | Supports multi-turn from UltraChat data; builds on previous context. |
| Context-Aware Translation | "Translate this sentence from English to German, considering the dialogue history: {history}\nEnglish: May I know your email for the PRS-ORG Account please?\nGerman:" | Dialogue settings | Incorporates history (e.g., triples) for coherence in chats. |
| Terminology-Aware | "Translate from English to German using these terms: 'avalanche beacons' -> 'Lawinensuchgeräte'.\nEnglish: All were wearing avalanche beacons.\nGerman:" | Domain-specific accuracy | Reduces errors by enforcing glossaries, as in medical/technical texts. |
| Post-Editing | "Post-edit this machine translation from English to German, making minimal changes to correct errors.\nEnglish: Good, but would like to find something better\nMT: Gut, aber ich würde gerne etwas Besseres finden.\nPost-edited:" | APE for quality improvement | Focuses on small adjustments; use with analysis for error explanations. |
| NER Tagging | "Study this taxonomy: Person (Names of people), Location (Places)...\nIdentify entities in tokens: [\"el\", \"republicano\", \"emilio\", \"castelar\"...]\nAdd B- to first token, I- to subsequent, O for others.\nAnswer:" | Entity recognition | From TowerBlocks; detailed taxonomy aids precision in multilingual NER. |
| Error Explanation & Correction | "You are provided with Source, Translation, quality analysis, score. Generate explanation for each error and a correct translation.\nEnglish: All were wearing avalanche beacons.\nGerman: Alle trugen Lawinenschilder.\nAnalysis: Alle trugen <error1 severity=“major”>Lawinenschilder</error1>.\nScore: weak" | Quality-aware MT | Combines analysis with corrections; useful for post-editing workflows. |

Additional practices: Use positive directives, delimiters for segments, and role cues sparingly; for domains like medical, integrate external glossaries (e.g., from IATE). In code, set `do_sample=False` for reproducibility and `max_new_tokens=256+` based on needs. While versatile, the model is not for general chat or code; verify outputs for cultural/dialectal fit, especially in low-resource pairs.

This synthesis draws from model cards, research papers, and implementations, balancing zero/few-shot approaches for robust multilingual translation.

### Key Citations
- [Unbabel/TowerInstruct-7B-v0.2 on Hugging Face](https://huggingface.co/Unbabel/TowerInstruct-7B-v0.2)
- [TowerInstruct 7B V0.2 on Dataloop](https://dataloop.ai/library/model/unbabel_towerinstruct-7b-v02/)
- [Unbabel/TowerBlocks-v0.1 on Hugging Face](https://huggingface.co/datasets/Unbabel/TowerBlocks-v0.1)
- [Post-editing Discussion on Hugging Face](https://huggingface.co/Unbabel/TowerInstruct-7B-v0.2/discussions/8)
- [SAP-samples GitHub Repository](https://github.com/SAP-samples/quality-aware-decoding-translation)
- [Graph Representations for MT (WMT PDF)](https://aclanthology.org/2024.wmt-1.106.pdf)
- [Tower: An Open Multilingual LLM (arXiv PDF)](https://arxiv.org/pdf/2402.17733)
- [An Open Multilingual LLM (OpenReview PDF)](https://openreview.net/pdf/70a305afbfd7d351108bffe8f608fd359d8092a9.pdf)
- [xTower: MT with Error Explanations (arXiv)](https://arxiv.org/pdf/2406.19482)
