namespace AIInfluenceAntigravity.Transport
{
    public sealed class ChatTurn
    {
        public string Prompt { get; }
        public string SplitSystem { get; set; }
        public string SplitUser { get; set; }
        public bool HasSplit => !string.IsNullOrEmpty(SplitSystem) && !string.IsNullOrEmpty(SplitUser);

        public ChatTurn(string prompt)
        {
            Prompt = prompt ?? string.Empty;
        }

        public static ChatTurn FromPrompt(string prompt, string splitSystem = null, string splitUser = null)
        {
            var turn = new ChatTurn(prompt);
            turn.SplitSystem = splitSystem;
            turn.SplitUser = splitUser;
            return turn;
        }
    }
}
