using SPPR.Models;

namespace SPPR.Services;

public class RecognitionResult
{
    public List<int> Scores { get; set; } = new();
    public int WinnerIndex { get; set; }
    public string ExplanationText { get; set; } = string.Empty;
}

public class DecisionService
{
    public RecognitionResult Recognize(KnowledgeBase knowledgeBase, IReadOnlyList<bool> answers)
    {
        var result = new RecognitionResult();
        var bestScore = int.MinValue;
        var winnerIndex = 0;

        for (var i = 0; i < knowledgeBase.Objects.Count; i++)
        {
            var score = 0;
            for (var k = 0; k < knowledgeBase.Features.Count; k++)
            {
                var answerValue = answers[k] ? 1 : 0;
                score += knowledgeBase.Weights[i][k] * answerValue;
            }

            result.Scores.Add(score);
            if (score > bestScore)
            {
                bestScore = score;
                winnerIndex = i;
            }
        }

        result.WinnerIndex = winnerIndex;
        result.ExplanationText = BuildExplanation(knowledgeBase, answers, winnerIndex);
        return result;
    }

    private static string BuildExplanation(KnowledgeBase knowledgeBase, IReadOnlyList<bool> answers, int winnerIndex)
    {
        var lines = new List<string>();
        lines.Add($"Переможець: {knowledgeBase.Objects[winnerIndex].Name}");
        lines.Add("Ознаки з відповіддю 'так' та їх внесок:");

        var hasSelectedFeatures = false;
        for (var k = 0; k < knowledgeBase.Features.Count; k++)
        {
            if (!answers[k])
            {
                continue;
            }

            hasSelectedFeatures = true;
            var featureName = knowledgeBase.Features[k].Name;
            var weight = knowledgeBase.Weights[winnerIndex][k];
            lines.Add($"- {featureName}: вага {weight}");
        }

        if (!hasSelectedFeatures)
        {
            lines.Add("- Жодної ознаки не обрано.");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
