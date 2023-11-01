#nullable enable
using System;
using System.Collections.Generic;

namespace SlobViewer.Kaikki
{
  public class Word
  {
    // Name of the word, i.e. the word itself
    public string Name;
    public List<WordForm> Forms = new List<WordForm>();

    public override string ToString()
    {
      return Name;
    }
  }

  public class WordForm
  {
    public string pos;
    public List<Sense> Senses = new List<Sense>();
    public List<Form>? Forms;
    public List<string>? Derived;
    public List<string>? Sounds;
    public List<string>? Related;

    public override string ToString()
    {
      return pos;
    }
  }

  public class Sense
  {
    public string[] Text = Array.Empty<string>();
    public List<Example>? Examples;
    public string[] Form_Of = Array.Empty<string>();

    public override string ToString()
    {
      return Examples is not null ?
        $"{string.Join("; ", Text)} ({Examples.Count} examples)" :
        string.Join("; ", Text);
    }
  }

  public class Example
  {
    public string Text;
    public string? Translation;

    public override string ToString()
    {
      return $"\"{Text}\" : \"{Translation}\"";
    }
  }

  public class Form
  {
    public string Text;
    public string[] Tags = Array.Empty<string>();

    public override string ToString()
    {
      return $"{Text} : {string.Join("; ", Tags)}";
    }
  }
}
