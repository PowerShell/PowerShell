using System;

class EmojiTest
{
    static int CodePointLengthInBufferCells(int codePoint)
    {
        // Emoji and symbol ranges (most emojis are wide/2-cell)
        if ((codePoint >= 0x1F300 && codePoint <= 0x1F9FF) || 
            (codePoint >= 0x1F000 && codePoint <= 0x1F02F) || 
            (codePoint >= 0x1F0A0 && codePoint <= 0x1F0FF) || 
            (codePoint >= 0x1F100 && codePoint <= 0x1F64F) || 
            (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) || 
            (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) || 
            (codePoint >= 0x1FA00 && codePoint <= 0x1FA6F) || 
            (codePoint >= 0x1FA70 && codePoint <= 0x1FAFF) || 
            (codePoint >= 0x2600 && codePoint <= 0x26FF) ||   
            (codePoint >= 0x2700 && codePoint <= 0x27BF) ||   
            (codePoint >= 0x1F170 && codePoint <= 0x1F251) || 
            (codePoint >= 0x1F3FB && codePoint <= 0x1F3FF) || 
            (codePoint >= 0x20000 && codePoint <= 0x2FFFD) || 
            (codePoint >= 0x30000 && codePoint <= 0x3FFFD))   
        {
            return 2;
        }

        if (codePoint <= 0xFFFF)
        {
            return CharLengthInBufferCells((char)codePoint);
        }

        return 2;
    }

    static int CharLengthInBufferCells(char c)
    {
        // Check for BMP emojis that are 2 cells wide
        if ((c >= 0x2600 && c <= 0x26FF) ||  
            (c >= 0x2700 && c <= 0x27BF) ||  
            (c >= 0x2300 && c <= 0x23FF) ||  
            (c >= 0x2B50 && c <= 0x2B55))    
        {
            return 2;
        }

        bool isWide = c >= 0x1100 &&
            (c <= 0x115f || 
             c == 0x2329 || c == 0x232a ||
             ((uint)(c - 0x2e80) <= (0xa4cf - 0x2e80) &&
              c != 0x303f) || 
             ((uint)(c - 0xac00) <= (0xd7a3 - 0xac00)) || 
             ((uint)(c - 0xf900) <= (0xfaff - 0xf900)) || 
             ((uint)(c - 0xfe10) <= (0xfe19 - 0xfe10)) || 
             ((uint)(c - 0xfe30) <= (0xfe6f - 0xfe30)) || 
             ((uint)(c - 0xff00) <= (0xff60 - 0xff00)) || 
             ((uint)(c - 0xffe0) <= (0xffe6 - 0xffe0)));

        return 1 + (isWide ? 1 : 0);
    }

    static int CalculateLength(string str)
    {
        int length = 0;
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            
            if (char.IsHighSurrogate(c) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
            {
                int codePoint = char.ConvertToUtf32(c, str[i + 1]);
                length += CodePointLengthInBufferCells(codePoint);
                i++; // Skip the low surrogate
            }
            else
            {
                length += CharLengthInBufferCells(c);
            }
        }
        return length;
    }

    static void Main()
    {
        Console.WriteLine("Testing emoji width calculation:");
        Console.WriteLine();
        
        string[] testStrings = {
            "✅", "⛔", "🛶", "🌵", 
            "Yes", "No", "Canoe", "Cactus"
        };

        foreach (var str in testStrings)
        {
            int width = CalculateLength(str);
            Console.WriteLine($"String: '{str}' | Calculated Width: {width} | Actual Length: {str.Length}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Full table test:");
        Console.WriteLine();
        
        string[] row1 = { "✅", "Yes", "🛶", "Canoe" };
        string[] row2 = { "⛔", "No", "🌵", "Cactus" };
        
        Console.WriteLine("Column widths with emoji-aware calculation:");
        for (int i = 0; i < row1.Length; i++)
        {
            Console.WriteLine($"Column {i}: '{row1[i]}' = {CalculateLength(row1[i])} cells, '{row2[i]}' = {CalculateLength(row2[i])} cells");
        }
    }
}
