using System;

namespace SlobViewer.Collections.Text
{
  /// <summary>
  /// Determination of the longest common substring of two strings.
  /// </summary>
  public class LongestCommonSubstringOfTwoStrings
  {
    private int[][] _lcsArray = new int[0][];
    private int _allocatedLengthFirstString;
    private int _allocatedLengthSecondString;

    /// <summary>
    /// Determines the length of the longest common substring of two strings.
    /// </summary>
    /// <param name="firstString">The first string.</param>
    /// <param name="secondString">The second string.</param>
    /// <returns>The length of the longest common substring of the two strings.</returns>
    public int LongestCommonSubstring(string firstString, string secondString)
    {
      int firstLength = firstString.Length;
      int secondLength = secondString.Length;
      Reallocate(firstLength + 1, secondLength + 1);
      int result = 0;
      for (int i = 0; i <= firstLength; i++)
      {
        for (int j = 0; j <= secondLength; j++)
        {
          if (i == 0 || j == 0)
          {
            if (j < secondLength)
              _lcsArray[i][j] = 0;
          }
          else if (firstString[i - 1] == secondString[j - 1])
          {
            _lcsArray[i][j] = _lcsArray[i - 1][j - 1] + 1;

            result = Math.Max(result, _lcsArray[i][j]);
          }
          else
          {
            _lcsArray[i][j] = 0;
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Pre-allocates helper space in order to avoid multiple allocations when a instance of this class is used repeatedly.
    /// </summary>
    /// <param name="maxLengthOfFirstString">The maximum possible length of the first string.</param>
    /// <param name="maxLengthOfSecondString">The maximum possible length of the second string.</param>
    public void Preallocate(int maxLengthOfFirstString, int maxLengthOfSecondString)
    {
      Reallocate(maxLengthOfFirstString + 1, maxLengthOfSecondString + 1);
    }


    private void Reallocate(int lengthFirstString, int lengthSecondString)
    {
      if (lengthFirstString > _allocatedLengthFirstString)
      {
        int[][] oldArr = _lcsArray;

        _allocatedLengthFirstString = lengthFirstString;
        _lcsArray = new int[_allocatedLengthFirstString][];
        Array.Copy(oldArr, _lcsArray, oldArr.Length);
        if (lengthSecondString <= _allocatedLengthSecondString)
        {
          for (int i = _lcsArray.Length + 1; i < lengthFirstString; ++i)
          {
            _lcsArray[i] = new int[_allocatedLengthSecondString];
          }
        }
      }

      if (lengthSecondString > _allocatedLengthSecondString)
      {
        _allocatedLengthSecondString = lengthSecondString;

        for (int i = 0; i < _lcsArray.Length; ++i)
        {
          _lcsArray[i] = new int[_allocatedLengthSecondString];
        }
      }
    }
  }
}
