using Newtonsoft.Json;

namespace API.Palantir.Helpers
{
    public static class NoteChunkHelper
    {
        public static List<List<T>> MaxTextLengthSplit<T>(
            List<T> items,
            int maxLen)
        {
            var result = new List<List<T>>();
            var current = new List<T>();
            int currentLength = 0;

            foreach (var item in items)
            {
                string json = JsonConvert.SerializeObject(item);
                int itemLength = json.Length;

                if (current.Any() && currentLength + itemLength > maxLen)
                {
                    result.Add(current);
                    current = new List<T>();
                    currentLength = 0;
                }

                current.Add(item);
                currentLength += itemLength;
            }

            if (current.Any())
                result.Add(current);

            return result;
        }
    }
}
