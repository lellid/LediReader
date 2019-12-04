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

      stb.AppendLine("Inner Html:");
      stb.AppendLine(te.InnerText);


      return stb.ToString();
    }
  }
}
