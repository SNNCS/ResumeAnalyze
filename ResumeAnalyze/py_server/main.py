import os
import json
import re
from typing import List, Optional, Dict, Any

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from dotenv import load_dotenv

import numpy as np
from sklearn.metrics.pairwise import cosine_similarity

os.environ["OPENAI_API_KEY"] = "sk-30Dyi0KPAUEa2qoi3Q1RClmFBpeV9jHpvpanERIs60aJxoit"

from langchain_openai import ChatOpenAI, OpenAIEmbeddings
from langchain_core.prompts import ChatPromptTemplate

# local embeddings
from sentence_transformers import SentenceTransformer

app = FastAPI(title="Resume-JD Analyzer (LangChain)")

# Allow the local ASP.NET frontend to make cross-origin calls
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], allow_credentials=True,
    allow_methods=["*"], allow_headers=["*"],
)


# Pydantic request/response models
class AnalyzeRequest(BaseModel):
    resume: str
    jd: str
    use_openai: bool = False
    enable_rerank: bool = False  # Placeholder for rerank toggle
    language: str = "en"          

class ScoreBlock(BaseModel):
    Skills: float
    Experience: float
    Education: float
    Keywords: float
    ATS: float

class AnalyzeResponse(BaseModel):
    score: float
    verdict: str
    keywords_hit: int
    job_title: str
    scores: ScoreBlock
    summary: Optional[str] = ""
    bullets: Optional[str] = ""
    radar_chart_json: str
    bar_chart_json: str
    pairsJson: str  # JSON-stringified list of {resume, jd, sim}

# Utility functions
_SKILL_SEEDS = [
    "python","java","c++","sql","pytorch","tensorflow","scikit-learn",
    "langchain","faiss","chromadb","nlp","rag","agent",
    "azure","aws","docker","kubernetes","react","asp.net","linux","git","streamlit"
]

def naive_skill_extract(text: str) -> List[str]:
    t = re.sub(r"[^a-zA-Z0-9+.# ]+", " ", text.lower())
    found = []
    for s in _SKILL_SEEDS:
        pattern = r"\b" + re.escape(s).replace(r"\+\+", r"\+\+") + r"\b"
        if re.search(pattern, t):
            found.append(s)
    return sorted(set(found))

def sentence_split(text: str) -> List[str]:
    parts = re.split(r"[\n\.]", text)
    return [p.strip() for p in parts if len(p.split()) >= 6][:50]

def get_embeddings_backend(use_openai: bool):
    if use_openai and os.getenv("OPENAI_API_KEY"):
        return OpenAIEmbeddings(), None
    st_model = SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2")
    return None, st_model

def embed_texts(texts: List[str], emb_backend, st_model):
    if emb_backend is not None:
        # OpenAIEmbeddings
        vecs = [emb_backend.embed_query(t) for t in texts]
        return np.array(vecs, dtype="float32")
    else:
        return st_model.encode(texts, normalize_embeddings=True)

def cosine(a: np.ndarray, b: np.ndarray) -> float:
    a = a.reshape(1, -1); b = b.reshape(1, -1)
    sim = float(cosine_similarity(a, b)[0][0])
    return max(0.0, min(1.0, sim))

def score_components(resume_txt: str, jd_txt: str) -> Dict[str, float]:
    # Skills
    res_sk = set(naive_skill_extract(resume_txt))
    jd_sk  = set(naive_skill_extract(jd_txt))
    skill_cover = len(res_sk & jd_sk) / (len(jd_sk) + 1e-9)

    # Keywords
    must = [k for k in ["sql","python","pytorch","tensorflow","langchain"] if k in jd_txt.lower()]
    kw_hit = sum(1 for k in must if k in resume_txt.lower()) / (len(must) + 1e-9)

    # Experience
    yrs_need = 2 if re.search(r"\b(2\+|two\+|at least 2)\b", jd_txt.lower()) else \
               1 if re.search(r"\b(1\+|one\+|at least 1)\b", jd_txt.lower()) else 0
    yrs_have = 2 if re.search(r"\b(2 years|two years|>1 year)\b", resume_txt.lower()) else \
               1 if re.search(r"\b(intern|project|months)\b", resume_txt.lower()) else 0
    exp_match = 1.0 if yrs_have >= yrs_need else (0.6 if (yrs_need==2 and yrs_have==1) else 0.3)

    # Education
    edu_match = 1.0 if re.search(r"\b(computer science|ai|data science)\b", resume_txt.lower()) else 0.6

    # ATS/Format
    bullets = len(re.findall('^\\s*[-*\\u2022]', resume_txt, re.M))
    numbers = len(re.findall(r"\d+%", resume_txt))
    ats_score = min(1.0, 0.3 + 0.05*bullets + 0.1*min(numbers, 3))

    return {
        "Skills": float(skill_cover),
        "Keywords": float(kw_hit),
        "Experience": float(exp_match),
        "Education": float(edu_match),
        "ATS": float(ats_score)
    }

def weighted_total(scores: Dict[str,float], w=(0.35,0.30,0.10,0.15,0.10)) -> float:
    keys = ["Skills","Experience","Education","Keywords","ATS"]
    s = sum(scores[k]*w[i] for i,k in enumerate(keys))
    return round(100.0 * s / (sum(w)+1e-9), 1)

def verdict_from_score(score: float) -> str:
    if score >= 75: return "Ready"
    if score >= 55: return "Improve"
    return "Major Gap"

def build_radar_json(scores: Dict[str,float]) -> str:
    # Chart.js config
    data = {
        "type": "radar",
        "data": {
            "labels": ["Skills","Experience","Education","Keywords","ATS"],
            "datasets": [{
                "label": "Match",
                "data": [
                    round(scores["Skills"]*100,1),
                    round(scores["Experience"]*100,1),
                    round(scores["Education"]*100,1),
                    round(scores["Keywords"]*100,1),
                    round(scores["ATS"]*100,1),
                ],
                "fill": True
            }]
        }
    }
    return json.dumps(data, ensure_ascii=False)

def build_bar_json(gaps: List[Dict[str, Any]]) -> str:
    labels = [g["skill"] for g in gaps]
    vals = [round(float(g["importance"])*100,1) for g in gaps]
    data = {
        "type": "bar",
        "data": {
            "labels": labels,
            "datasets": [{
                "label": "Importance",
                "data": vals
            }]
        }
    }
    return json.dumps(data, ensure_ascii=False)

def top_missing_skills(resume_txt: str, jd_txt: str, topk: int = 10) -> List[Dict[str,Any]]:
    res_sk = set(naive_skill_extract(resume_txt))
    jd_sk  = list(naive_skill_extract(jd_txt))
    # Simplified importance: 1.0 if it appears in the JD, otherwise 0.7
    gaps = [{"skill": s, "importance": (1.0 if s in jd_txt.lower() else 0.7)} for s in jd_sk if s not in res_sk]
    gaps.sort(key=lambda x: x["importance"], reverse=True)
    return gaps[:topk]

def evidence_pairs(resume_txt: str, jd_txt: str, use_openai: bool) -> List[Dict[str,Any]]:
    sents_r = sentence_split(resume_txt)[:12]
    sents_j = sentence_split(jd_txt)[:12]
    if not sents_r or not sents_j:
        return []

    emb_backend, st_model = get_embeddings_backend(use_openai)
    R = embed_texts(sents_r, emb_backend, st_model)
    J = embed_texts(sents_j, emb_backend, st_model)

    pairs = []
    for i, r in enumerate(sents_r):
        sims = cosine_similarity(R[i:i+1], J)[0]
        j = int(np.argmax(sims))
        pairs.append({"resume": r, "jd": sents_j[j], "sim": float(sims[j])})
    pairs.sort(key=lambda x: x["sim"], reverse=True)
    return pairs[:5]

def call_llm_summary_and_bullets(resume_txt: str, jd_txt: str, lang: str, use_openai: bool):
    key_ok = bool(os.getenv("OPENAI_API_KEY")) and use_openai
    if not key_ok:
        gaps = ", ".join(x["skill"] for x in top_missing_skills(resume_txt, jd_txt)[:5]) or "(no obvious gaps)"
        summary = f"Summary generated from heuristic scoring: overall fit is moderate. Main gaps: {gaps}. Recommend adding JD-aligned projects and quantified metrics to the resume."
        bullets = "- Built a Python/SQL data-cleaning pipeline that improved processing speed by 35%\n- Contributed to a RAG demo using LangChain + Chroma to deliver retrieval with citations"
        return summary, bullets

    try:
        llm = ChatOpenAI(model="gpt-4o-mini", temperature=0.2)

        if lang == "en":
            prompt_sum = ChatPromptTemplate.from_messages([
                ("system", "You are a senior recruiter. Produce a English summary comparing the candidate's resume and the JD, highlight strengths and gaps, and end with two action items."),
                ("human", "Resume:\n{resume}\n\nJD:\n{jd}\n")
            ])
        else:
            prompt_sum = ChatPromptTemplate.from_messages([
                ("system", "You are a senior recruiter. Produce an English summary comparing resume and JD, list strengths and gaps, end with 2 action items."),
                ("human", "Resume:\n{resume}\n\nJD:\n{jd}\n")
            ])
        summary = llm.invoke(prompt_sum.format(resume=resume_txt, jd=jd_txt)).content

        # Bullets
        if lang == "zh":
            prompt_bul = ChatPromptTemplate.from_messages([
                ("system", "You are a resume optimization assistant. Provide three English bullet points that start with an action verb, include numbers or impact, and are ready to paste into the resume."),
                ("human", "Candidate resume:\n{resume}\n\nJD:\n{jd}\n")
            ])
        else:
            prompt_bul = ChatPromptTemplate.from_messages([
                ("system", "Resume optimizer: Write 3 metric-driven bullets in English, action-first, tailored to the JD."),
                ("human", "Resume:\n{resume}\n\nJD:\n{jd}\n")
            ])
        bullets = llm.invoke(prompt_bul.format(resume=resume_txt, jd=jd_txt)).content

        return summary, bullets

    except Exception as e:
        err = f"[LLM call failed: {type(e).__name__}] {e}"
        fallback = "- LLM call failed, so show placeholder bullets from the rule-based logic.\n- Verify the API key, model name, and network connectivity."
        return err, fallback


# Endpoints
@app.get("/debug/llm_state")
def llm_state():
    import os
    return {
        "has_env_key": bool(os.getenv("OPENAI_API_KEY")),
        "use_openai_flag_hint": "LLM only runs when the request sets use_openai=true",
        "model": "gpt-4o-mini"
    }

@app.get("/health")
def health():
    return {"ok": True}

@app.post("/api/analyze", response_model=AnalyzeResponse)
def analyze(req: AnalyzeRequest):
    print(">>> received.use_openai =", req.use_openai)
    print(">>> received.resume[:40] =", (req.resume or "")[:40])
    print(">>> received.jd[:40]     =", (req.jd or "")[:40])

    resume_txt = (req.resume or "").strip()
    jd_txt = (req.jd or "").strip()
    if not resume_txt or not jd_txt:
        return AnalyzeResponse(
            score=0, verdict="Major Gap", keywords_hit=0, job_title="Unknown",
            scores=ScoreBlock(Skills=0,Experience=0,Education=0,Keywords=0,ATS=0),
            summary="Missing input text.", bullets="", radar_chart_json="{}", bar_chart_json="{}", pairsJson="[]"
        )

    #Scoring
    comp = score_components(resume_txt, jd_txt)
    total = weighted_total(comp)
    verdict = verdict_from_score(total)
    keywords_hit = int(round(comp["Keywords"] * 10))  # Rough conversion for display only

    #Skill gaps and evidence pairs
    gaps = top_missing_skills(resume_txt, jd_txt, topk=10)
    pairs = evidence_pairs(resume_txt, jd_txt, use_openai=req.use_openai)

    #LLM summary and bullets
    summary, bullets = call_llm_summary_and_bullets(resume_txt, jd_txt, req.language, req.use_openai)

    #Chart.js config
    radar_json = build_radar_json(comp)
    bar_json = build_bar_json(gaps)

    #Derive job title from the first line of the JD
    first_line = jd_txt.splitlines()[0].strip() if jd_txt else "Unknown Role"
    job_title = first_line[:40] + ("..." if len(first_line) > 40 else "")

    #Evidence sentence pairs as JSON
    pairs_json = json.dumps(pairs, ensure_ascii=False)

    return AnalyzeResponse(
        score=float(total),
        verdict=verdict,
        keywords_hit=keywords_hit,
        job_title=job_title,
        scores=ScoreBlock(
            Skills=round(comp["Skills"]*100,1),
            Experience=round(comp["Experience"]*100,1),
            Education=round(comp["Education"]*100,1),
            Keywords=round(comp["Keywords"]*100,1),
            ATS=round(comp["ATS"]*100,1),
        ),
        summary=summary,
        bullets=bullets,
        radar_chart_json=radar_json,
        bar_chart_json=bar_json,
        pairsJson=pairs_json
    )

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="127.0.0.1", port=8000, reload=True, log_level="debug")
