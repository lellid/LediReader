using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

#nullable enable
namespace SlobViewer.Kaikki
{
  public class KaikkiReader
  {
    string _fileName;
    public KaikkiReader(string fileName)
    {
      _fileName = fileName;
    }


    /// <summary>
    /// Reads a json file from kaikki.org and returns a dictionary, with the word as key, and one or more Json nodes as value.
    /// The Json nodes must afterwards be parsed to a dom tree.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<JsonNode>> Read()
    {
      var dict = new Dictionary<string, List<JsonNode>>();

      using (var stream = new StreamReader(_fileName))
      {
        string line;
        while (null != (line = stream.ReadLine()))
        {
          var node = JsonNode.Parse(line);
          var word = node["word"].ToString();
          if (!dict.ContainsKey(word))
          {
            dict.Add(word, new List<JsonNode>());
          }

          dict[word].Add(node);
        }
      }
      return dict;
    }
  }
}
