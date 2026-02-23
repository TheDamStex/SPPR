using System.Text.Json;
using SPPR.Models;

namespace SPPR.Services;

public class KnowledgeStorageService
{
    // Зберігаємо knowledge.json поруч з exe (поточна робоча директорія застосунку).
    // Для навчальної роботи це простіше для перевірки: файл легко знайти, видалити або підмінити.
    private const string FileName = "knowledge.json";

    public string GetKnowledgePath()
    {
        return Path.Combine(AppContext.BaseDirectory, FileName);
    }

    public bool Exists()
    {
        return File.Exists(GetKnowledgePath());
    }

    public KnowledgeBase? Load()
    {
        var path = GetKnowledgePath();
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<KnowledgeBase>(json);
    }

    public void Save(KnowledgeBase knowledgeBase)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(knowledgeBase, options);
        File.WriteAllText(GetKnowledgePath(), json);
    }
}
