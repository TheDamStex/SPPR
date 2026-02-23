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

        if (knowledgeBase.Objects.Count == 0)
        {
            result.WinnerIndex = -1;
            result.ExplanationText = "База знань не містить жодного об'єкта для розпізнавання.";
            return result;
        }

        var bestScore = int.MinValue;
        var winnerIndex = -1;

        for (var i = 0; i < knowledgeBase.Objects.Count; i++)
        {
            var score = 0;
            for (var k = 0; k < knowledgeBase.Features.Count; k++)
            {
                var answerValue = k < answers.Count && answers[k] ? 1 : 0;
                score += GetWeightSafe(knowledgeBase, i, k) * answerValue;
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

    private static int GetWeightSafe(KnowledgeBase knowledgeBase, int objectIndex, int featureIndex)
    {
        if (objectIndex < 0 || objectIndex >= knowledgeBase.Weights.Count)
        {
            return 0;
        }

        var objectWeights = knowledgeBase.Weights[objectIndex];
        if (featureIndex < 0 || featureIndex >= objectWeights.Count)
        {
            return 0;
        }

        return objectWeights[featureIndex];
    }

    private static string BuildExplanation(KnowledgeBase knowledgeBase, IReadOnlyList<bool> answers, int winnerIndex)
    {
        var lines = new List<string>();

        if (winnerIndex < 0 || winnerIndex >= knowledgeBase.Objects.Count)
        {
            lines.Add("Не вдалося визначити переможця через некоректні дані бази знань.");
            return string.Join(Environment.NewLine, lines);
        }

        lines.Add($"Переможець: {knowledgeBase.Objects[winnerIndex].Name}");
        lines.Add("Ознаки з відповіддю 'так' та їх внесок:");

        var hasSelectedFeatures = false;
        for (var k = 0; k < knowledgeBase.Features.Count; k++)
        {
            if (k >= answers.Count || !answers[k])
            {
                continue;
            }

            hasSelectedFeatures = true;
            var featureName = knowledgeBase.Features[k].Name;
            var weight = GetWeightSafe(knowledgeBase, winnerIndex, k);
            lines.Add($"- {featureName}: вага {weight}");
        }

        if (!hasSelectedFeatures)
        {
            lines.Add("- Жодної ознаки не обрано.");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
