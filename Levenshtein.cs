using System;

namespace LocalLibrary
{
    public class Levenshtein
    {
        public string Name;
        public int Value;

        public int Distance(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return string.IsNullOrEmpty(b) ? 0 : b.Length;
            }
            if (string.IsNullOrEmpty(b))
            {
                return a.Length;
            }
            int lengthA = a.Length;
            int lengthB = b.Length;
            int[,] matrix = new int[lengthA + 1, lengthB + 1];
            for (int i = 0; i <= lengthA; i++)
            {
                matrix[i, 0] = i;
            }
            for (int i = 0; i <= lengthB; i++)
            {
                matrix[0, i] = i;
            }
            for (int i = 1; i <= lengthA; i++)
            {
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            return matrix[lengthA, lengthB];
        }
    }
}
