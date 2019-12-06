// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LediReader.Gui
{
  public class XHtmlTreeInspectorController : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    #region Bindable properties

    string _propertyText;
    public string PropertyText
    {
      get => _propertyText;
      set
      {
        if (!(_propertyText == value))
        {
          _propertyText = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PropertyText)));
        }
      }
    }

    XmlNode _domRootElement;
    public XmlNode DomRootElement
    {
      set
      {
        if (!object.ReferenceEquals(_domRootElement, value))
        {
          _domRootElement = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DomRootElement)));
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tree1stLevelElements)));
        }
      }
    }

    public IEnumerable<System.Xml.XmlNode> Tree1stLevelElements
    {
      get
      {
        if (null != _domRootElement)
        {
          foreach (var ele in _domRootElement.ChildNodes)
            yield return (System.Xml.XmlNode)ele;
        }
      }
    }


    XmlNode _selectedTextElement;
    public XmlNode SelectedTextElement
    {
      set
      {
        if (!object.ReferenceEquals(_selectedTextElement, value))
        {
          _selectedTextElement = value;
          PropertyText = GetPropertyText(_selectedTextElement);

          if (null == SelectedHierarchyOfInitiallySelectedItem && null != _selectedTextElement)
            SetInitialSelectedItem(_selectedTextElement);
        }
      }
    }

    public List<XmlNode> SelectedHierarchyOfInitiallySelectedItem { get; private set; }
    private void SetInitialSelectedItem(XmlNode textElement)
    {
      var list = new List<XmlNode>();
      list.Add(textElement);
      while (textElement.ParentNode != null)
      {
        list.Add(textElement.ParentNode);
        textElement = textElement.ParentNode;
      }
      SelectedHierarchyOfInitiallySelectedItem = list;
    }


    #endregion

    string GetPropertyText(XmlNode te)
    {
      if (null == te)
        return "Nothing selected";

      var stb = new StringBuilder();

      stb.AppendLine($"{te.Name} {te.LocalName}");

      if (!(te.Attributes is null))
      {
        foreach (XmlAttribute att in te.Attributes)
        {
          stb.AppendLine($"{att.Name}: {att.Value}");
        }
      }

      var innerXml = te.InnerXml;
      var innerXmlTrimmed = innerXml.Trim();

      if (!string.IsNullOrEmpty(innerXmlTrimmed))
      {
        stb.AppendLine("Inner Xml:");
        stb.AppendLine(te.InnerXml);
      }
      else if (!string.IsNullOrEmpty(innerXml))
      {
        stb.AppendLine("Inner Xml:");
        foreach (var c in innerXml)
          WhiteSpaceToText(c, stb);
        stb.AppendLine();
      }
      else // Inner Xml is empty
      {
        var innerText = te.InnerText;
        var innerTextTrimmed = innerText.Trim();

        if (!string.IsNullOrEmpty(innerTextTrimmed))
        {
          stb.AppendLine("Inner text:");
          stb.AppendLine(te.InnerText);
        }
        else if (!string.IsNullOrEmpty(innerText))
        {
          stb.AppendLine("Inner text:");
          foreach (var c in innerText)
            WhiteSpaceToText(c, stb);
          stb.AppendLine();
        }
        else
        {
          stb.AppendLine("(Both Inner Xml and Inner text are empty)");
        }
      }


      return stb.ToString();
    }

    static void WhiteSpaceToText(char whitespace, StringBuilder stb)
    {
      switch (whitespace)
      {
        case ' ':
          stb.Append("<space>");
          break;
        case '\t':
          stb.Append("<tab>");
          break;
        case '\r':
          stb.Append("<CR>");
          break;
        case '\n':
          stb.Append("<LF>");
          break;
      }
    }

  }
}
