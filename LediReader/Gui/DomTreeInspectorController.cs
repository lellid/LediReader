// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LediReader.Gui
{
  public class DomTreeInspectorController : INotifyPropertyChanged
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

    HtmlToFlowDocument.Dom.TextElement _domRootElement;
    public HtmlToFlowDocument.Dom.TextElement DomRootElement
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

    public IEnumerable<HtmlToFlowDocument.Dom.TextElement> Tree1stLevelElements
    {
      get
      {
        return _domRootElement?.Childs;
      }
    }


    HtmlToFlowDocument.Dom.TextElement _selectedTextElement;
    public HtmlToFlowDocument.Dom.TextElement SelectedTextElement
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

    public List<HtmlToFlowDocument.Dom.TextElement> SelectedHierarchyOfInitiallySelectedItem { get; private set; }
    private void SetInitialSelectedItem(HtmlToFlowDocument.Dom.TextElement textElement)
    {
      var list = new List<HtmlToFlowDocument.Dom.TextElement>();
      list.Add(textElement);
      while (textElement.Parent != null)
      {
        list.Add(textElement.Parent);
        textElement = textElement.Parent;
      }
      SelectedHierarchyOfInitiallySelectedItem = list;
    }


    #endregion

    string GetPropertyText(HtmlToFlowDocument.Dom.TextElement te)
    {
      if (null == te)
        return "Nothing selected";

      var stb = new StringBuilder();

      stb.AppendLine(te.GetType().Name);

      // properties of a TextElement
      if (te.Background.HasValue)
      {
        stb.Append("Background: ");
        stb.AppendLine(te.Background.Value.ToString());
      }

      if (te.Foreground.HasValue)
      {
        stb.Append("Foreground: ");
        stb.AppendLine(te.Foreground.Value.ToString());
      }

      if (!string.IsNullOrEmpty(te.FontFamily))
      {
        stb.Append("FontFamily: ");
        stb.AppendLine(te.FontFamily);
      }

      if (te.FontSize.HasValue)
      {
        stb.Append("FontSize: ");
        stb.AppendLine(te.FontSize.Value.ToString());
      }
      else if (te.FontSizeLocalOrInherited.HasValue)
      {
        stb.Append("FontSize (inherited): ");
        stb.AppendLine(te.FontSizeLocalOrInherited.Value.ToString());
      }

      if (te.FontStyle.HasValue)
      {
        stb.Append("FontStyle: ");
        stb.AppendLine(te.FontStyle.Value.ToString());
      }

      if (te.FontWeight.HasValue)
      {
        stb.Append("FontWeight: ");
        stb.AppendLine(te.FontWeight.Value.ToString());
      }

      if (te is HtmlToFlowDocument.Dom.Block block)
      {
        if (block.LineHeight.HasValue)
        {
          stb.Append("LineHeight: ");
          stb.AppendLine(block.LineHeight.Value.ToString());
        }

        if (block.TextAlignment.HasValue)
        {
          stb.Append("TextAlignment: ");
          stb.AppendLine(block.TextAlignment.Value.ToString());
        }

        if (block.Margin.HasValue)
        {
          stb.Append("Margin: ");
          stb.AppendLine(block.Margin.Value.ToString());
        }


        if (block.Padding.HasValue)
        {
          stb.Append("Padding: ");
          stb.AppendLine(block.Padding.Value.ToString());
        }


        if (block.BorderBrush.HasValue)
        {
          stb.Append("BorderBrush: ");
          stb.AppendLine(block.BorderBrush.Value.ToString());
        }

        if (block.BorderThickness.HasValue)
        {
          stb.Append("BorderThickness: ");
          stb.AppendLine(block.BorderThickness.Value.ToString());
        }
      }

      switch (te)
      {
        case HtmlToFlowDocument.Dom.Run run:
          stb.Append("Text: ");
          stb.AppendLine(run.Text);
          break;
      }

      return stb.ToString();
    }
  }
}
