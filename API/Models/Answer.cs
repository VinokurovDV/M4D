namespace API.Models
{
    [Serializable]
    public class Answer
    {
        public string Token { get; set; }

        public Answer(string token)
        {
            Token = token;
        }
    }
}
