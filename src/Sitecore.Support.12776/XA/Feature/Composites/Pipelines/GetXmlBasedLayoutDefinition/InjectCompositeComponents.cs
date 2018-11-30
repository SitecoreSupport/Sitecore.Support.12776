using Microsoft.Extensions.DependencyInjection;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Mvc.Extensions;
using Sitecore.Mvc.Pipelines.Response.GetXmlBasedLayoutDefinition;
using Sitecore.XA.Foundation.Caching;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Presentation.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Sitecore.Support.XA.Feature.Composites.Pipelines.GetXmlBasedLayoutDefinition
{
  public class InjectCompositeComponents : Sitecore.XA.Feature.Composites.Pipelines.GetXmlBasedLayoutDefinition.InjectCompositeComponents
  {
    public override void Process(GetXmlBasedLayoutDefinitionArgs args)
    {
      #region FIX 12776
      Item obj = args.ContextItem ?? args.PageContext.Item ?? Sitecore.Mvc.Presentation.PageContext.Current.Item;
      #endregion

      XElement result = args.Result;
      if (result == null || !obj.Paths.IsContentItem)
        return;
      Item siteItem = ServiceLocator.ServiceProvider.GetService<IMultisiteContext>().GetSiteItem(obj);
      if (siteItem == null)
        return;
      IEnumerable<XElement> compositeComponents = this.GetCompositeComponents(result);
      if (!compositeComponents.Any<XElement>())
        return;
      DictionaryCacheValue dictionaryCacheValue = this.DictionaryCache.Get(this.CreateCompositesXmlCacheKey(obj.ID, siteItem.ID));
      if (this.PageMode.IsNormal && dictionaryCacheValue != null && dictionaryCacheValue.Properties.ContainsKey((object)"CompositesXml"))
      {
        args.Result = XElement.Parse(dictionaryCacheValue.Properties[(object)"CompositesXml"].ToString());
      }
      else
      {
        if (!args.CustomData.ContainsKey("sxa-composite-recursion-level"))
          args.CustomData.Add("sxa-composite-recursion-level", (object)1);
        else
          args.CustomData["sxa-composite-recursion-level"] = (object)((int)args.CustomData["sxa-composite-recursion-level"] + 1);
        foreach (XElement rendering in compositeComponents)
          this.ProcessCompositeComponent(args, rendering, result);
        List<XElement> list = result.Descendants((XName)"d").ToList<XElement>();
        args.Result.Descendants((XName)"d").Remove<XElement>();
        args.Result.Add((object)list);
        bool flag = false;
        foreach (DeviceModel devices in new LayoutModel(args.Result.ToString()).Devices.DevicesCollection)
        {
          flag = devices.Renderings.RenderingsCollection.ToList<Sitecore.XA.Foundation.Presentation.Layout.RenderingModel>().Any<Sitecore.XA.Foundation.Presentation.Layout.RenderingModel>((Func<Sitecore.XA.Foundation.Presentation.Layout.RenderingModel, bool>)(rm => rm.XmlNode.FindChildNode((Predicate<XmlNode>)(node => node.Name.Equals("rls"))) != null));
          if (flag)
            break;
        }
        if (!this.PageMode.IsNormal || flag)
          return;
        this.StoreValueInCache(this.CreateCompositesXmlCacheKey(obj.ID, siteItem.ID), args.Result.ToString());
      }
    }
  }
}
