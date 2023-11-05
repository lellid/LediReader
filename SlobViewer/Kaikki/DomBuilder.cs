using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

#nullable enable
namespace SlobViewer.Kaikki
{
  public class DomBuilder
  {

    public List<Word> BuildDom(Dictionary<string, List<JsonNode>> allWords, bool destroyDictionary)
    {
      var result = new List<Word>();
      var keys = allWords.Keys.ToArray();
      foreach (var key in keys)
      {
        var dstWord = BuildWord(key, allWords[key]);
        if (destroyDictionary)
        {
          allWords.Remove(key);
        }
        result.Add(dstWord);
      }
      return result;
    }

    public Word BuildWord(string name, List<JsonNode> wordNodes)
    {
      var dstWord = new Word() { Name = name };
      foreach (var node in wordNodes)
      {
        var dstForm = BuildWordForm(node);
        dstWord.Forms.Add(dstForm);
      }
      return dstWord;
    }

    private WordForm BuildWordForm(JsonNode formNode)
    {
      var dstWordForm = new WordForm();
      dstWordForm.pos = formNode["pos"].ToString();

      {
        // Build senses
        var sensesObj = formNode["senses"];
        if (sensesObj is JsonArray senses)
        {
          foreach (var senseObj in senses)
          {
            var dstSense = BuildSense(senseObj);
            dstWordForm.Senses.Add(dstSense);
          }
        }
        else if (sensesObj is not null)
        {
          var dstSense = BuildSense(sensesObj);
          dstWordForm.Senses.Add(dstSense);
        }
      }

      {
        // Build forms
        var formsObj = formNode["forms"];
        if (formsObj is JsonArray forms)
        {
          foreach (var form in forms)
          {
            var dstForm = BuildForm(form);
            AddForm(dstWordForm, dstForm);
          }
        }
        else if (formsObj is not null)
        {
          throw new NotImplementedException();
        }
      }

      {
        // Build derived
        // derived is an array of Json object, each containing 'word' property
        var derivedObj = formNode["derived"];
        if (derivedObj is JsonArray derivedArray)
        {
          foreach (var derived in derivedArray)
          {
            var dstDerived = BuildDerived(derived);
            AddDerived(dstWordForm, dstDerived);
          }
        }
        else if (derivedObj is not null)
        {
          throw new NotImplementedException();
        }
      }

      {
        // Build related
        // related is an array of Json object, each containing 'word' property
        var relatedObj = formNode["related"];
        if (relatedObj is JsonArray relatedArray)
        {
          foreach (var related in relatedArray)
          {
            var dstRelated = BuildRelated(related);
            AddRelated(dstWordForm, dstRelated);
          }
        }
        else if (relatedObj is not null)
        {
          throw new NotImplementedException();
        }
      }

      {
        // Build sounds
        // sounds is an array of Json object, each containing 'ipa' property
        var soundsObj = formNode["sounds"];
        if (soundsObj is JsonArray soundsArray)
        {
          foreach (var sounds in soundsArray)
          {
            var dstSound = BuildSound(sounds);
            AddSound(dstWordForm, dstSound);
          }
        }
        else if (soundsObj is not null)
        {
          throw new NotImplementedException();
        }
      }

      return dstWordForm;
    }

    private Sense BuildSense(JsonNode senseObj)
    {
      var dstSense = new Sense();
      var glossObj = senseObj["glosses"];
      if (glossObj is JsonArray glosses)
      {
        dstSense.Text = glosses.Select(x => x.ToString()).ToArray();
      }
      else if (glossObj is not null)
      {
        throw new NotImplementedException();
      }

      var formOfObj = senseObj["form_of"];
      if (formOfObj is JsonArray formOfArr)
      {
        dstSense.Form_Of = Build_Form_Of(formOfArr);
      }
      else if (formOfObj is not null)
      {
        throw new NotImplementedException();
      }

      var exampleObj = senseObj["examples"];
      if (exampleObj is JsonArray examples)
      {
        foreach (var example in examples)
        {
          if (example is not null)
          {
            var dstExample = BuildExample(example);
            AddExample(dstSense, dstExample);
          }
        }
      }
      else if (exampleObj is not null)
      {
        throw new NotImplementedException();
      }

      return dstSense;
    }

    private void AddExample(Sense dstSense, Example? example)
    {
      if (example is not null)
      {
        dstSense.Examples ??= new List<Example>();
        dstSense.Examples.Add(example);
      }
    }

    private Example? BuildExample(JsonNode exampleObj)
    {
      var text = exampleObj["text"]?.ToString();
      var translation = exampleObj["english"]?.ToString() ?? exampleObj["german"]?.ToString();

      if (text is not null)
      {
        return new Example { Text = text, Translation = translation };
      }
      else
      {
        return null;
      }
    }

    private void AddForm(WordForm wordForm, Form? form)
    {
      if (form is null)
      {
        return;
      }

      wordForm.Forms ??= new List<Form>();
      wordForm.Forms.Add(form);
    }

    private Form? BuildForm(JsonNode formObj)
    {
      var text = formObj["form"]?.ToString();

      if (text is null)
      {
        return null;
      }

      var tags = formObj["tags"];

      if (tags is JsonArray tagsArray)
      {
        return new Form { Text = text, Tags = tagsArray.Select(t => t.ToString()).ToArray() };
      }
      else
      {
        return new Form { Text = text, Tags = new string[0] };
      }
    }

    private void AddDerived(WordForm wordForm, string? derived)
    {
      if (derived is null || string.IsNullOrEmpty(derived))
      {
        return;
      }

      wordForm.Derived ??= new List<string>();
      wordForm.Derived.Add(derived);
    }

    private string? BuildDerived(JsonNode formObj)
    {
      return formObj["word"]?.ToString();
    }

    private void AddSound(WordForm wordForm, string? sound)
    {
      if (sound is null || string.IsNullOrEmpty(sound))
      {
        return;
      }

      wordForm.Sounds ??= new List<string>();
      wordForm.Sounds.Add(sound);
    }

    private string? BuildSound(JsonNode soundObj)
    {
      return soundObj["ipa"]?.ToString();
    }

    private void AddRelated(WordForm wordForm, string? related)
    {
      if (related is null || string.IsNullOrEmpty(related))
      {
        return;
      }

      wordForm.Related ??= new List<string>();
      wordForm.Related.Add(related);
    }

    private string? BuildRelated(JsonNode relatedObj)
    {
      return relatedObj["word"]?.ToString();
    }

    private string[] Build_Form_Of(JsonArray jsonArray)
    {
      // the Json array may consist of JsonNodes, with word as tag
      return jsonArray.Select(x => (x is JsonNode node ? node["word"]?.ToString() : x?.ToString()) ?? string.Empty).ToArray();
    }
  }
}
