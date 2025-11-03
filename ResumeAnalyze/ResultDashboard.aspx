<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ResultDashboard.aspx.cs" Inherits="ResumeAnalyze.ResultDashboard" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Analysis Results Dashboard</title>
    <meta charset="utf-8" />
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        body { font-family: Segoe UI, Arial; background: #f5f5f5; margin: 0; padding: 25px; }
        .container { background: white; max-width: 900px; margin: auto; padding: 25px; border-radius: 10px; box-shadow: 0 0 10px #ccc; }
        h2 { margin-bottom: 10px; }
        .section { margin-top: 20px; }
        .label { font-weight: bold; }
        .btn { padding: 8px 16px; border: none; border-radius: 6px; margin-right: 10px; cursor: pointer; }
        .btn-primary { background-color: #0078D7; color: white; }
        .btn-gray { background-color: #ccc; }
        .metrics { display: flex; justify-content: space-between; flex-wrap: wrap; }
        .metric-box { background: #f8f8f8; padding: 10px 15px; margin: 8px 0; border-radius: 6px; width: 48%; }
        .bullets, .summary { background: #f8f8f8; padding: 12px; border-radius: 6px; white-space: pre-wrap; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { padding: 8px; border-bottom: 1px solid #ccc; text-align: left; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <h2>Resume Analysis Dashboard</h2>
            <p>Your AI-generated evaluation and scores.</p>

            <div class="metrics">
                <div class="metric-box"><span class="label">Overall Score:</span> <asp:Label ID="LblScore" runat="server" /></div>
                <div class="metric-box"><span class="label">Verdict:</span> <asp:Label ID="LblVerdict" runat="server" /></div>
                <div class="metric-box"><span class="label">Keywords Matched:</span> <asp:Label ID="LblKeywords" runat="server" /></div>
                <div class="metric-box"><span class="label">Analyzed At:</span> <asp:Label ID="LblTime" runat="server" /></div>
            </div>

            <div class="section">
                <h3>Charts</h3>
                <h5>Radar Chart</h5>
                <canvas id="radarCanvas" width="600" height="360"></canvas>
                <h5>Bar Chart</h5>
                <canvas id="barCanvas" width="600" height="360"></canvas>

                <asp:Literal ID="LitRadarJson" runat="server" Visible="false"></asp:Literal>
                <asp:Literal ID="LitBarJson" runat="server" Visible="false"></asp:Literal>

                <script>
                    (function () {
                        function parseJsonSafe(text) {
                            if (!text) return null;
                            try { return JSON.parse(text); } catch (e) { return null; }
                        }

                        var radarSpec = parseJsonSafe('<%= (LitRadarJson.Text ?? "").Replace("\\","\\\\").Replace("'","\\'") %>');
                  var barSpec   = parseJsonSafe('<%= (LitBarJson.Text   ?? "").Replace("\\","\\\\").Replace("'","\\'") %>');

                        if (radarSpec && radarSpec.type === 'radar') {
                            var rc = document.getElementById('radarCanvas').getContext('2d');
                            new Chart(rc, radarSpec);
                        }

                        // 渲染柱状图（如果有）
                        if (barSpec && barSpec.type === 'bar') {
                            var bc = document.getElementById('barCanvas').getContext('2d');
                            new Chart(bc, barSpec);
                        }
                    })();
                </script>
            </div>

            <div class="section">
                <h3>Top Evidence Matches</h3>
                <asp:Repeater ID="RptPairs" runat="server">
                    <HeaderTemplate>
                        <table>
                            <thead>
                                <tr>
                                    <th>Score</th>
                                    <th>Resume Sentence</th>
                                    <th>Job Description Sentence</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td><%# Eval("Score") %></td>
                            <td><%# Eval("ResumeSent") %></td>
                            <td><%# Eval("JDSent") %></td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>

            <div class="section">
                <h3>Key Bullet Points</h3>
                <div class="bullets"><asp:Literal ID="LitBullets" runat="server"></asp:Literal></div>
            </div>

            <div class="section">
                <h3>AI Summary</h3>
                <div class="summary"><asp:Literal ID="LitSummary" runat="server"></asp:Literal></div>
            </div>

            <div class="section">
                <asp:Button ID="BtnReAnalyze" runat="server" CssClass="btn btn-primary" Text="Re-Analyze Another Resume" OnClick="BtnReAnalyze_Click" />
                <asp:Button ID="BtnSaveReport" runat="server" CssClass="btn btn-gray" Text="Save Report" OnClick="BtnSaveReport_Click" />
                <asp:Button ID="BtnRegenerateBullets" runat="server" CssClass="btn btn-gray" Text="Regenerate Bullets" OnClick="BtnRegenerateBullets_Click" />
                <asp:Button ID="BtnCopyBullets" runat="server" CssClass="btn btn-gray" Text="Copy Bullets" OnClick="BtnCopyBullets_Click" />
            </div>
        </div>
    </form>
</body>
</html>
