using System.Text;
using System.Xml;

#nullable enable
namespace SlobViewer.Kaikki
{
  public class XHtmlWriter
  {
    public string GetXHtml(Word word, bool partially)
    {
      var stb = new StringBuilder();
      var writerSettings = new XmlWriterSettings()
      {
        Encoding = Encoding.UTF8,
        OmitXmlDeclaration = partially,
        Indent = true,
      };
      using (var writer = XmlWriter.Create(stb, writerSettings))
      {

        if (!partially)
        {
          writer.WriteStartDocument();
          writer.WriteRaw("\r\n");
          writer.WriteRaw("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
          writer.WriteRaw("\r\n");
          writer.WriteRaw("<?xml-stylesheet type=\"text/css\" href=\"style.css\"?>");
        }

        writer.WriteStartElement("html", "http://www.w3.org/1999/xhtml");
        {
          writer.WriteStartElement("head");
          {
            writer.WriteElementString("title", word.Name);
            writer.WriteRaw("<link rel=\"stylesheet\" type=\"text/css\" href=\"style.css\" />");
          }
          writer.WriteEndElement(); // head

          writer.WriteStartElement("body");
          {
            writer.WriteElementString("h1", word.Name);

            foreach (var form in word.Forms)
            {
              WriteWordForm(form, writer);
            }
          }
          writer.WriteEndElement(); // body
        }
        writer.WriteEndElement(); // html

        if (!partially)
        {
          writer.WriteEndDocument();
        }
      }

      return stb.ToString();
    }

    private void WriteWordForm(WordForm form, XmlWriter writer)
    {
      writer.WriteElementString("h2", form.pos);

      if (form.Sounds is not null && form.Sounds.Count > 0)
      {
        if (form.Sounds.Count == 1)
        {
          writer.WriteElementString("p", form.Sounds[0]);
        }
        else
        {
          writer.WriteElementString("p", string.Join("; ", form.Sounds.ToArray()));
        }
      }

      // ------- Senses -----------------------
      if (form.Senses.Count > 0)
      {
        foreach (var sense in form.Senses)
        {
          WriteSense(sense, writer);
        }
      }

      // -------------- Forms -----------------------

      if (form.Forms is not null && form.Forms.Count > 0)
      {
        writer.WriteStartElement("p");
        {
          writer.WriteElementString("b", "Forms:");
        }
        writer.WriteEndElement();

        foreach (var fo in form.Forms)
        {
          WriteForm(fo, writer);
        }
      }

      // ------------- Derived -----------------------
      if (form.Derived is not null && form.Derived.Count > 0)
      {
        writer.WriteElementString("p", "Derived:");
        if (form.Derived.Count == 1)
        {
          writer.WriteElementString("p", form.Derived[0]);
        }
        else
        {
          writer.WriteElementString("p", string.Join("; ", form.Derived.ToArray()));
        }
      }

      // ------------- Related -----------------------
      if (form.Related is not null && form.Related.Count > 0)
      {
        writer.WriteElementString("p", "Related:");
        if (form.Related.Count == 1)
        {
          writer.WriteElementString("p", form.Related[0]);
        }
        else
        {
          writer.WriteElementString("p", string.Join("; ", form.Related.ToArray()));
        }
      }


    }



    private void WriteSense(Sense sense, XmlWriter writer)
    {
      if (sense.Form_Of.Length > 0)
      {
        writer.WriteStartElement("p");
        {
          writer.WriteElementString("b", "Form of: ");
          if (sense.Form_Of.Length == 1)
          {
            writer.WriteString(sense.Form_Of[0]);
          }
          else
          {
            writer.WriteString(string.Join("; ", sense.Form_Of));
          }
        }
        writer.WriteEndElement(); // p
      }

      if (sense.Text.Length > 0)
      {
        foreach (var t in sense.Text)
        {
          writer.WriteElementString("h3", t);
        }
      }

      if (sense.Examples is not null && sense.Examples.Count > 0)
      {

        if (sense.Examples.Count == 1)
        {
          var example = sense.Examples[0];
          writer.WriteStartElement("p");
          {
            writer.WriteElementString("b", "Example: ");
            writer.WriteString(example.Text);
            if (!string.IsNullOrEmpty(example.Translation))
            {
              writer.WriteString(" - ");
              writer.WriteString(example.Translation);
            }
          }
          writer.WriteEndElement(); // p
        }
        else // more than one example
        {
          writer.WriteStartElement("p");
          {
            writer.WriteElementString("b", "Examples: ");
          }
          writer.WriteEndElement(); // p
          foreach (var example in sense.Examples)
          {
            writer.WriteStartElement("p");
            {
              writer.WriteString(example.Text);
              if (!string.IsNullOrEmpty(example.Translation))
              {
                writer.WriteString(" - ");
                writer.WriteString(example.Translation);
              }
            }
            writer.WriteEndElement(); // p
          }
        }
      }
    }

    private void WriteForm(Form form, XmlWriter writer)
    {
      writer.WriteStartElement("p");
      {
        writer.WriteElementString("b", form.Text);
        if (form.Tags.Length > 0)
        {
          writer.WriteString(": ");
          writer.WriteString(string.Join("; ", form.Tags));
        }


      }
      writer.WriteEndElement();
    }
  }
}
