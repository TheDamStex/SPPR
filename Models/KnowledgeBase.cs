namespace SPPR.Models;

public class KnowledgeBase
{
    public List<FoodObject> Objects { get; set; } = new();
    public List<Feature> Features { get; set; } = new();
    public List<List<int>> Weights { get; set; } = new();
}
