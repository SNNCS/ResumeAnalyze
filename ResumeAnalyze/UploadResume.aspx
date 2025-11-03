<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="UploadResume.aspx.cs" Inherits="ResumeAnalyze.UploadResume" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>AI Resume Analyzer</title>
    <meta charset="utf-8" />
    <style>
        body { font-family: Segoe UI, Arial; background: #f5f5f5; margin: 0; padding: 30px; }
        .container { background: white; max-width: 850px; margin: auto; padding: 25px; border-radius: 10px; box-shadow: 0 0 10px #ccc; }
        textarea { width: 100%; height: 160px; margin-bottom: 12px; padding: 8px; font-size: 14px; }
        .label { font-weight: bold; margin-top: 10px; display: block; }
        .btn { padding: 10px 18px; border: none; border-radius: 6px; margin-right: 10px; cursor: pointer; }
        .btn-analyze { background-color: #0078D7; color: white; }
        .btn-clear { background-color: #ccc; }
        .status { margin-top: 15px; color: #555; font-style: italic; }
        .checkboxes { margin: 10px 0; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <h2>AI Resume Analyzer</h2>
            <p>Compare your resume with a job description to evaluate how well you match.</p>

            <span class="label">Paste Your Resume:</span>
            <asp:TextBox ID="ResumeTextBox" runat="server" TextMode="MultiLine" placeholder="Paste your resume text here..."></asp:TextBox>
            <asp:FileUpload ID="ResumeFileUpload" runat="server" />

            <span class="label">Paste Job Description (JD):</span>
            <asp:TextBox ID="JdTextBox" runat="server" TextMode="MultiLine" placeholder="Paste the job description text here..."></asp:TextBox>

            <span class="label">Job URL (Optional):</span>
            <asp:TextBox ID="JdUrlTextBox" runat="server" placeholder="Paste job URL (optional)..."></asp:TextBox>

            <div class="checkboxes">
                <asp:CheckBox ID="ChkUseOpenAI" runat="server" Text="Use OpenAI Analysis" Checked="true" />
                <br />
                <asp:CheckBox ID="ChkEnableRerank" runat="server" Text="Enable Reranking (optional)" />
            </div>

            <asp:Button ID="BtnAnalyze" runat="server" CssClass="btn btn-analyze" Text="Analyze Resume" OnClick="BtnAnalyze_Click" />
            <asp:Button ID="BtnClear" runat="server" CssClass="btn btn-clear" Text="Clear All" OnClick="BtnClear_Click" />

            <div class="status">
                <asp:Label ID="LblStatus" runat="server" Text=""></asp:Label>
            </div>
        </div>
    </form>
</body>
</html>
