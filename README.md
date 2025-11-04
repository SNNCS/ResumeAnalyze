# AI Resume Analyzer

## Overview
AI Resume Analyzer is an intelligent system designed to analyze resumes and job descriptions using natural language processing and machine learning techniques.  
It extracts key skills, evaluates similarity between resumes and job requirements, and presents results through an interactive ASP.NET dashboard.

This project demonstrates full-stack integration between a Python-based AI backend and a .NET web interface, showcasing end-to-end design, data processing, and visualization.

---

## Objectives
- Automatically evaluate candidate–job fit based on text similarity.
- Generate AI-driven summaries and bullet points highlighting match quality.
- Provide a visual dashboard for recruiters or students to understand analysis results.
- Demonstrate integration between AI inference (FastAPI) and traditional ASP.NET WebForms.

---

## Features
- Resume and Job Description semantic similarity scoring.
- AI-generated summaries and bullet point suggestions.
- Visualization of top evidence matches using Chart.js.
- Modular backend API for analysis (FastAPI + Python).
- WebForms frontend for displaying structured results.
- Support for English and multilingual resumes.

---

## Project Structure
```
ResumeAnalyze/
├── ResumeAnalyze.sln
├── ResumeAnalyze/              # ASP.NET WebForms project
│   ├── resultdashboard.aspx
│   ├── resultdashboard.aspx.cs
│   ├── web.config
│   ├── /bin
│   ├── /App_Data
│   └── ...
├── DS/                         # Data science analysis or notebook files
│   ├── model_test.ipynb
│   └── analysis_notes.txt
├── resume/                     # Sample resume text files
│   ├── resume_sample_1.txt
│   ├── resume_sample_2.txt
│   └── resume_sample_3.txt
├── jd/                         # Sample job description text files
│   ├── jd_sample_1.txt
│   ├── jd_sample_2.txt
│   └── jd_sample_3.txt
├── screenshots/                # System UI and result screenshots
│   ├── dashboard.png
│   └── result_page.png
├── .gitignore
└── README.md
```

---

## Technologies Used
| Layer | Technology |
|--------|-------------|
| Frontend | ASP.NET WebForms, HTML, CSS, JavaScript, Chart.js |
| Backend | Python, FastAPI |
| AI / ML | OpenAI API, Embeddings, Cosine Similarity |
| Data Handling | JSON, Pandas, scikit-learn |
| IDE / Tools | Visual Studio, VS Code, GitHub |
| Version Control | Git |

---

## Installation and Setup

### 1. Clone the Repository
```bash
git clone https://github.com/SNNCS/ResumeAnalyze.git
cd ResumeAnalyze
```

### 2. Set up the FastAPI Backend
Navigate to your backend folder (or create `app/main.py` for FastAPI).

Install dependencies:
```bash
pip install fastapi uvicorn openai scikit-learn numpy pandas
```

Run the backend server:
```bash
uvicorn app.main:app --reload
```
Backend will be available at:
```
http://127.0.0.1:8000/api/analyze
```

### 3. Run the ASP.NET Frontend
Open `ResumeAnalyze.sln` in Visual Studio.

Select **resultdashboard.aspx** as the start page and press **F5** to run the project.  
Ensure the backend URL in your code matches the FastAPI server address.

---

## Input and Output Examples

### Example Input
- **Resume**: `resume/resume_sample_1.txt`
- **Job Description**: `jd/jd_sample_1.txt`

### Example Output (Dashboard Display)
- Overall Similarity Score: 0.83  
- Top Matched Skills: Python, FastAPI, Machine Learning  
- AI Summary:
  ```
  The candidate demonstrates strong alignment with backend development and AI model integration.
  Experience in FastAPI and TensorFlow matches the job requirements closely.
  ```

---

## Sample Data

### Resumes (`/resume`)
| File | Description |
|------|--------------|
| resume_sample_1.txt | AI & Software Engineering Intern |
| resume_sample_2.txt | Data Analyst Intern |
| resume_sample_3.txt | Full Stack Developer Intern |

### Job Descriptions (`/jd`)
| File | Description |
|------|--------------|
| jd_sample_1.txt | AI & Software Engineering Intern |
| jd_sample_2.txt | Data Analyst Intern |
| jd_sample_3.txt | Full Stack Developer Intern |

---

## Demonstration

Screenshots of the dashboard and results are available in the `/screenshots` folder.  
To include your own:
- Capture the result page after analysis.
- Save as `dashboard.png` or `result_page.png`.
- Reference them in this README for visual presentation.

---

## Future Improvements
- Multi-job comparison (one resume vs multiple JDs)
- Integration with vector database for scalable retrieval
- Improved reranking and reasoning models
- Modern React-based frontend alternative
