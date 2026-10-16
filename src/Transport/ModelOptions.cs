namespace AIInfluenceAntigravity.Transport
{
    public sealed class ModelOptions
    {
        public string Model { get; }
        public float Temperature { get; }
        public int MaxTokens { get; }

        public ModelOptions(string model, float temperature = 1.0f, int maxTokens = 0)
        {
            Model = model;
            Temperature = temperature;
            MaxTokens = maxTokens;
        }
    }
}
