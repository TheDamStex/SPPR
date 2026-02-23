using SPPR.Models;

namespace SPPR.Services;

public class TrainingResult
{
    public bool WeightsChanged { get; set; }
    public string ChangesText { get; set; } = string.Empty;
}

public class TrainingService
{
    public TrainingResult Train(KnowledgeBase knowledgeBase, IReadOnlyList<bool> answers, int predictedIndex, int trueIndex)
    {
        var result = new TrainingResult();

        if (predictedIndex == trueIndex)
        {
            result.WeightsChanged = false;
            result.ChangesText = "Прогноз правильний. Ваги не змінено.";
            return result;
        }

        var lines = new List<string>
        {
            $"Прогноз був: {knowledgeBase.Objects[predictedIndex].Name}",
            $"Правильний об'єкт: {knowledgeBase.Objects[trueIndex].Name}",
            "Зміни ваг для активних ознак:"
        };

        for (var k = 0; k < knowledgeBase.Features.Count; k++)
        {
            if (!answers[k])
            {
                continue;
            }

            var featureName = knowledgeBase.Features[k].Name;

            var oldTrueWeight = knowledgeBase.Weights[trueIndex][k];
            knowledgeBase.Weights[trueIndex][k] = oldTrueWeight + 1;

            var oldPredictedWeight = knowledgeBase.Weights[predictedIndex][k];
            var newPredictedWeight = oldPredictedWeight - 1;
            if (newPredictedWeight < 0)
            {
                newPredictedWeight = 0;
            }

            knowledgeBase.Weights[predictedIndex][k] = newPredictedWeight;

            lines.Add($"- {featureName}: {knowledgeBase.Objects[trueIndex].Name} {oldTrueWeight}->{knowledgeBase.Weights[trueIndex][k]}, " +
                      $"{knowledgeBase.Objects[predictedIndex].Name} {oldPredictedWeight}->{knowledgeBase.Weights[predictedIndex][k]}");
        }

        result.WeightsChanged = true;
        result.ChangesText = string.Join(Environment.NewLine, lines);
        return result;
    }
}
