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
    //protected static DictionaryCache DictionaryCacheInstance { get; } = new DictionaryCache("SXA[CompositesXml]", StringUtil.ParseSizeString(Sitecore.Configuration.Settings.GetSetting("XA.Feature.Composites.CompositesXmlCacheMaxSize", "50MB")));

    //protected DictionaryCache DictionaryCache
    //{
    //  get
    //  {
    //    return InjectCompositeComponents.DictionaryCacheInstance;
    //  }
    //}

    //protected IPageMode PageMode { get; } = ServiceLocator.ServiceProvider.GetService<IPageMode>();

    //protected IContext Context { get; } = ServiceLocator.ServiceProvider.GetService<IContext>();

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

    //public virtual IEnumerable<XElement> GetCompositeComponents(XElement layoutXml)
    //{
    //  List<XElement> xelementList = new List<XElement>();
    //  IList<XElement> devices = this.GetDevices(layoutXml, this.Context.Device.ID);
    //  ICompositeService service = ServiceLocator.ServiceProvider.GetService<ICompositeService>();
    //  foreach (XContainer xcontainer in (IEnumerable<XElement>)devices)
    //  {
    //    foreach (XElement descendant in xcontainer.Descendants((XName)"r"))
    //    {
    //      ID id = this.IdAttribute(descendant);
    //      if (id != ID.Null && service.IsCompositeRendering(id, (Database)null))
    //        xelementList.Add(descendant);
    //    }
    //  }
    //  return (IEnumerable<XElement>)xelementList;
    //}

    //public virtual IList<XElement> GetDevices(XElement layoutXml, ID contextDeviceId)
    //{
    //  IList<XElement> source = this.FilterDevicesByDeviceId((IEnumerable<XElement>)layoutXml.Descendants((XName)"d").ToList<XElement>(), contextDeviceId);
    //  if (!source.Any<XElement>() && this.Context.Device.FallbackDevice != null)
    //    return this.GetDevices(layoutXml, this.Context.Device.FallbackDevice.ID);
    //  return source;
    //}

    //public virtual IList<XElement> FilterDevicesByDeviceId(IEnumerable<XElement> devices, ID deviceId)
    //{
    //  return (IList<XElement>)devices.Where<XElement>((Func<XElement, bool>)(element => new ID(element.GetAttributeValue("id")).Equals(deviceId))).ToList<XElement>();
    //}

    //protected virtual void ProcessCompositeComponent(GetXmlBasedLayoutDefinitionArgs args, XElement rendering, XElement layoutXml)
    //{
    //  XmlNode xmlNode = rendering.ToXmlNode();
    //  Sitecore.XA.Foundation.Presentation.Layout.RenderingModel renderingModel = new Sitecore.XA.Foundation.Presentation.Layout.RenderingModel(xmlNode);
    //  Item contextItem = args.PageContext.Item;
    //  string datasource = new RenderingReference(xmlNode, contextItem.Language, contextItem.Database).Settings.Rules.Count > 0 ? this.GetCustomizedRenderingDataSource(renderingModel, contextItem) : renderingModel.DataSource;
    //  if (string.IsNullOrEmpty(datasource))
    //  {
    //    Log.Warn("Composite component datasource is empty", (object)rendering);
    //  }
    //  else
    //  {
    //    if (datasource.StartsWith("local:", StringComparison.OrdinalIgnoreCase))
    //      contextItem = this.SwitchContextItem(rendering, contextItem);
    //    if (datasource.StartsWith("page:", StringComparison.OrdinalIgnoreCase))
    //      contextItem = this.Context.Item;
    //    Item obj = this.ResolveCompositeDatasource(datasource, contextItem);
    //    if (obj == null)
    //    {
    //      Log.Error("Composite component has a reference to non-existing datasource : " + datasource, (object)this);
    //    }
    //    else
    //    {
    //      int num = this.DetectDatasourceLoop(args, obj) ? 1 : 0;
    //      int dynamicPlaceholderId = this.ExtractDynamicPlaceholderId(args, rendering);
    //      string empty = string.Empty;
    //      if (rendering.Attribute((XName)"sid") != null)
    //        empty = rendering.Attribute((XName)"sid").Value;
    //      string placeholder = renderingModel.Placeholder;
    //      if (num == 0)
    //      {
    //        foreach (KeyValuePair<int, Item> composite in ServiceLocator.ServiceProvider.GetService<ICompositeService>().GetCompositeItems(obj).Select<Item, KeyValuePair<int, Item>>((Func<Item, int, KeyValuePair<int, Item>>)((item, idx) => new KeyValuePair<int, Item>(idx + 1, item))).ToList<KeyValuePair<int, Item>>())
    //        {
    //          if (!this.TryMergeComposites(args, rendering, layoutXml, composite, dynamicPlaceholderId, placeholder, empty))
    //            break;
    //        }
    //      }
    //      else
    //        this.AbortRecursivePipeline(args, rendering);
    //      this.RollbackAntiLoopCollection(args, obj);
    //    }
    //  }
    //}

    //protected virtual int ExtractDynamicPlaceholderId(GetXmlBasedLayoutDefinitionArgs args, XElement rendering)
    //{
    //  Sitecore.XA.Foundation.Presentation.Layout.RenderingModel rm = new Sitecore.XA.Foundation.Presentation.Layout.RenderingModel(rendering.ToXmlNode());
    //  string name = "DynamicPlaceholderId";
    //  int dynamicPlaceholderId;
    //  if (rm.Parameters[name] != null)
    //  {
    //    dynamicPlaceholderId = MainUtil.GetInt(rm.Parameters[name], 1);
    //  }
    //  else
    //  {
    //    dynamicPlaceholderId = this.GetDynamicPlaceholderId(args, rm);
    //    rm.Parameters.Add(name, dynamicPlaceholderId.ToString());
    //    rendering.SetAttributeValue((XName)"par", (object)new UrlString(rm.Parameters).ToString());
    //  }
    //  return dynamicPlaceholderId;
    //}

    //protected virtual int GetDynamicPlaceholderId(GetXmlBasedLayoutDefinitionArgs args, Sitecore.XA.Foundation.Presentation.Layout.RenderingModel rm)
    //{
    //  DeviceModel deviceModel1 = new LayoutModel(args.Result.ToString()).Devices.DevicesCollection.FirstOrDefault<DeviceModel>((Func<DeviceModel, bool>)(deviceModel => deviceModel.Renderings.RenderingsCollection.Any<Sitecore.XA.Foundation.Presentation.Layout.RenderingModel>((Func<Sitecore.XA.Foundation.Presentation.Layout.RenderingModel, bool>)(r => r.UniqueId.Equals(rm.UniqueId)))));
    //  if (deviceModel1 == null)
    //    return 1;
    //  int num = 0;
    //  foreach (Sitecore.XA.Foundation.Presentation.Layout.RenderingModel renderings in deviceModel1.Renderings.RenderingsCollection)
    //  {
    //    NameValueCollection parameters = renderings.Parameters;
    //    int result;
    //    if (parameters != null && ((IEnumerable<string>)parameters.AllKeys).Contains<string>("DynamicPlaceholderId") && (int.TryParse(parameters["DynamicPlaceholderId"], out result) && result > num))
    //      num = result;
    //  }
    //  return num + 1;
    //}

    //protected virtual string GetCustomizedRenderingDataSource(Sitecore.XA.Foundation.Presentation.Layout.RenderingModel renderingModel, Item contextItem)
    //{
    //  ID id = renderingModel.Id;
    //  string path = contextItem.Database.GetItem(id).Paths.Path;
    //  Sitecore.Mvc.Presentation.Rendering rendering = new Sitecore.Mvc.Presentation.Rendering();
    //  rendering.RenderingItemPath = path;
    //  rendering.Properties["RenderingXml"] = renderingModel.ToString();
    //  CustomizeRenderingArgs customizeRenderingArgs = new CustomizeRenderingArgs(rendering);
    //  CorePipeline.Run("mvc.customizeRendering", (PipelineArgs)customizeRenderingArgs, false);
    //  return customizeRenderingArgs.Rendering.DataSource;
    //}

    //protected virtual Item SwitchContextItem(XElement rendering, Item contextItem)
    //{
    //  ID result;
    //  if (ID.TryParse(rendering.GetAttributeValue("sid"), out result))
    //    contextItem = contextItem.Database.GetItem(result);
    //  return contextItem;
    //}

    //protected virtual void ResolveLocalDatasources(IEnumerable<XElement> renderings, Item compositeDataItem)
    //{
    //  foreach (XElement rendering in renderings)
    //  {
    //    XAttribute xattribute = rendering.Attribute((XName)"ds");
    //    if (rendering.HasAttributes && xattribute != null)
    //    {
    //      string relativePath = xattribute.Value;
    //      if (relativePath.StartsWith("local:", StringComparison.OrdinalIgnoreCase))
    //      {
    //        string str = ServiceLocator.ServiceProvider.GetService<ILocalDatasourceService>().ExpandPageRelativePath(relativePath, compositeDataItem.Paths.FullPath);
    //        rendering.SetAttributeValue((XName)"ds", (object)str);
    //      }
    //    }
    //  }
    //}

    //protected virtual bool TryMergeComposites(GetXmlBasedLayoutDefinitionArgs args, XElement rendering, XElement layoutXml, KeyValuePair<int, Item> composite, int dynamicPlaceholderId, string parentPlaceholder, string partialDesignId)
    //{
    //  bool loopDetected;
    //  IList<XElement> compositeRenderings = this.GetCompositeRenderings(args, composite, out loopDetected);
    //  if (loopDetected)
    //  {
    //    this.AbortRecursivePipeline(args, rendering);
    //    return false;
    //  }
    //  this.ResolveLocalDatasources((IEnumerable<XElement>)compositeRenderings, composite.Value);
    //  this.MergeComposites(layoutXml, (IEnumerable<XElement>)compositeRenderings, composite, dynamicPlaceholderId, parentPlaceholder, partialDesignId);
    //  return true;
    //}

    //protected virtual void AbortRecursivePipeline(GetXmlBasedLayoutDefinitionArgs args, XElement rendering)
    //{
    //  this.SetTrueAttribute(rendering, "cmps-loop");
    //  if ((int)args.CustomData["sxa-composite-recursion-level"] == 1)
    //    return;
    //  this.AbortPipeline(args);
    //}

    //protected virtual bool DetectDatasourceLoop(GetXmlBasedLayoutDefinitionArgs args, Item datasource)
    //{
    //  bool flag = false;
    //  if (args.CustomData.ContainsKey("sxa-composite-antiloop"))
    //  {
    //    HashSet<ID> idSet = args.CustomData["sxa-composite-antiloop"] as HashSet<ID>;
    //    if (idSet != null)
    //    {
    //      if (!idSet.Contains(datasource.ID))
    //      {
    //        idSet.Add(datasource.ID);
    //        args.CustomData["sxa-composite-antiloop"] = (object)idSet;
    //      }
    //      else
    //        flag = true;
    //    }
    //  }
    //  else if (this.Context.Item.Parent.ID == datasource.ID)
    //    flag = true;
    //  else
    //    args.CustomData.Add("sxa-composite-antiloop", (object)new HashSet<ID>((IEnumerable<ID>)new ID[1]
    //    {
    //      datasource.ID
    //    }));
    //  return flag;
    //}

    //protected virtual void RollbackAntiLoopCollection(GetXmlBasedLayoutDefinitionArgs args, Item datasource)
    //{
    //  if (!args.CustomData.ContainsKey("sxa-composite-antiloop"))
    //    return;
    //  HashSet<ID> idSet = args.CustomData["sxa-composite-antiloop"] as HashSet<ID>;
    //  if (idSet == null || !idSet.Contains(datasource.ID))
    //    return;
    //  idSet.Remove(datasource.ID);
    //}

    //protected virtual void AbortPipeline(GetXmlBasedLayoutDefinitionArgs args)
    //{
    //  if (!args.CustomData.ContainsKey("sxa-composite-loop-detected"))
    //    args.CustomData.Add("sxa-composite-loop-detected", (object)true);
    //  args.AbortPipeline();
    //}

    //protected virtual Item ResolveCompositeDatasource(string datasource)
    //{
    //  return this.ResolveCompositeDatasource(datasource, (Item)null);
    //}

    //protected virtual Item ResolveCompositeDatasource(string datasource, Item contextItem)
    //{
    //  ID result;
    //  if (ID.TryParse(datasource, out result))
    //    return this.Context.Database.Items[result];
    //  ResolveRenderingDatasourceArgs renderingDatasourceArgs = new ResolveRenderingDatasourceArgs(datasource);
    //  if (contextItem != null)
    //    renderingDatasourceArgs.CustomData.Add(nameof(contextItem), (object)contextItem);
    //  CorePipeline.Run("resolveRenderingDatasource", (PipelineArgs)renderingDatasourceArgs);
    //  return this.Context.Database.GetItem(renderingDatasourceArgs.Datasource);
    //}

    //protected virtual void MergeComposites(XElement layoutXml, IEnumerable<XElement> compositeRenderings, KeyValuePair<int, Item> composite, int dynamicPlaceholderId, string parentPh, string partialDesignId)
    //{
    //  List<XElement> list = compositeRenderings.ToList<XElement>();
    //  foreach (XElement xelement in list.Where<XElement>(new Func<XElement, bool>(this.IsValidRenderingNode)))
    //  {
    //    string relativePlaceholder = this.GetRelativePlaceholder(xelement, composite, dynamicPlaceholderId, parentPh);
    //    XAttribute xattribute = xelement.Attribute((XName)"ph");
    //    if (xattribute != null)
    //      xattribute.SetValue((object)relativePlaceholder);
    //    this.SetAttribute(xelement, "cmps-item", (object)composite.Value.ID);
    //    this.SetTrueAttribute(xelement, "cmps");
    //    this.HandleEmptyDatasources(composite.Value, xelement);
    //    this.SetPartialDesignId(xelement, partialDesignId);
    //    this.SetOriginalDataSource(xelement);
    //  }
    //  foreach (XElement element in layoutXml.Descendants((XName)"d").GroupBy<XElement, string>((Func<XElement, string>)(element => element.GetAttributeValue("id"))).Select<IGrouping<string, XElement>, XElement>((Func<IGrouping<string, XElement>, XElement>)(elements => elements.First<XElement>())))
    //  {
    //    DeviceModel deviceModel = new DeviceModel(element.ToXmlNode());
    //    foreach (XElement xelement in list.Where<XElement>((Func<XElement, bool>)(e => this.NotInjectedIntoDevice(new Sitecore.XA.Foundation.Presentation.Layout.RenderingModel(e.ToXmlNode()), deviceModel))))
    //      element.Add((object)xelement);
    //  }
    //}

    //protected virtual bool IsValidRenderingNode(XElement arg)
    //{
    //  if (arg.HasAttributes && arg.AttributeNotNull("ph"))
    //    return arg.AttributeNotNull("id");
    //  return false;
    //}

    //protected virtual bool NotInjectedIntoDevice(Sitecore.XA.Foundation.Presentation.Layout.RenderingModel rm, DeviceModel dm)
    //{
    //  return dm.Renderings.RenderingsCollection.All<Sitecore.XA.Foundation.Presentation.Layout.RenderingModel>((Func<Sitecore.XA.Foundation.Presentation.Layout.RenderingModel, bool>)(r =>
    //  {
    //    if (!(r.UniqueId != rm.UniqueId))
    //      return r.Placeholder != rm.Placeholder;
    //    return true;
    //  }));
    //}

    //private void SetAttribute(XElement composite, string attribute, object value)
    //{
    //  if (composite.Attribute((XName)attribute) != null)
    //    return;
    //  XAttribute xattribute = new XAttribute((XName)attribute, value);
    //  composite.Add((object)xattribute);
    //}

    //protected virtual void HandleEmptyDatasources(Item compositeItem, XElement compositeRendering)
    //{
    //  if (compositeRendering.Attribute((XName)"id") == null)
    //    return;
    //  string str = compositeItem.ID.ToString();
    //  XAttribute xattribute1 = compositeRendering.Attribute((XName)"ds");
    //  if (xattribute1 == null)
    //  {
    //    XAttribute xattribute2 = new XAttribute((XName)"ds", (object)str);
    //    compositeRendering.Add((object)xattribute2);
    //  }
    //  else if (string.IsNullOrEmpty(xattribute1.Value))
    //  {
    //    xattribute1.SetValue((object)str);
    //  }
    //  else
    //  {
    //    if (!xattribute1.Value.Contains("{CONTEXT_SWITCH}"))
    //      return;
    //    xattribute1.SetValue((object)"");
    //  }
    //}

    //protected virtual void SetTrueAttribute(XElement composite, string attribute)
    //{
    //  if (composite.Attribute((XName)attribute) != null)
    //    return;
    //  XAttribute xattribute = new XAttribute((XName)attribute, (object)"true");
    //  composite.Add((object)xattribute);
    //}

    //protected virtual IList<XElement> GetCompositeRenderings(GetXmlBasedLayoutDefinitionArgs args, KeyValuePair<int, Item> composite, out bool loopDetected)
    //{
    //  GetXmlBasedLayoutDefinitionArgs layoutDefinitionArgs = this.PreparePipelineArgs(args, composite);
    //  CorePipeline.Run("mvc.getCompositeXmlBasedLayoutDefinition", (PipelineArgs)layoutDefinitionArgs);
    //  List<XElement> xelementList = new List<XElement>();
    //  if (!layoutDefinitionArgs.CustomData.ContainsKey("sxa-composite-loop-detected"))
    //  {
    //    XElement deviceDefinition = this.GetValidDeviceDefinition(layoutDefinitionArgs.Result, layoutDefinitionArgs.PageContext.Device.DeviceItem.ID);
    //    if (deviceDefinition == null && layoutDefinitionArgs.PageContext.Device.DeviceItem.FallbackDevice != null)
    //      deviceDefinition = this.GetValidDeviceDefinition(layoutDefinitionArgs.Result, layoutDefinitionArgs.PageContext.Device.DeviceItem.FallbackDevice.ID);
    //    xelementList = deviceDefinition == null ? layoutDefinitionArgs.Result.Descendants((XName)"r").ToList<XElement>() : deviceDefinition.Descendants((XName)"r").ToList<XElement>();
    //  }
    //  loopDetected = layoutDefinitionArgs.Aborted && layoutDefinitionArgs.CustomData.ContainsKey("sxa-composite-loop-detected");
    //  return (IList<XElement>)xelementList;
    //}

    //protected virtual XElement GetValidDeviceDefinition(XElement layout, ID deviceId)
    //{
    //  return layout.Descendants((XName)"d").FirstOrDefault<XElement>((Func<XElement, bool>)(e =>
    //  {
    //    string attributeValueOrNull = e.GetAttributeValueOrNull("id", StringComparison.Ordinal);
    //    if (e.GetAttributeValueOrNull("l", StringComparison.Ordinal) != null && attributeValueOrNull != null)
    //      return new ID(attributeValueOrNull).Equals(deviceId);
    //    return false;
    //  }));
    //}

    //protected virtual GetXmlBasedLayoutDefinitionArgs PreparePipelineArgs(GetXmlBasedLayoutDefinitionArgs args, KeyValuePair<int, Item> composite)
    //{
    //  Sitecore.Mvc.Presentation.PageContext pageContext1 = args.PageContext;
    //  Sitecore.Mvc.Presentation.PageContext pageContext2 = new Sitecore.Mvc.Presentation.PageContext()
    //  {
    //    Device = pageContext1.Device,
    //    Database = pageContext1.Database,
    //    Item = composite.Value,
    //    RequestContext = pageContext1.RequestContext
    //  };
    //  GetXmlBasedLayoutDefinitionArgs layoutDefinitionArgs1 = new GetXmlBasedLayoutDefinitionArgs();
    //  layoutDefinitionArgs1.PageContext = pageContext2;
    //  layoutDefinitionArgs1.ProcessorItem = args.ProcessorItem;
    //  GetXmlBasedLayoutDefinitionArgs layoutDefinitionArgs2 = layoutDefinitionArgs1;
    //  layoutDefinitionArgs2.CustomData.AddRange(args.CustomData);
    //  if (!layoutDefinitionArgs2.CustomData.ContainsKey("sxa-composite"))
    //    layoutDefinitionArgs2.CustomData.Add("sxa-composite", (object)composite.Value);
    //  else
    //    layoutDefinitionArgs2.CustomData["sxa-composite"] = (object)composite.Value;
    //  return layoutDefinitionArgs2;
    //}

    //protected virtual string GetRelativePlaceholder(XElement compositeRendering, KeyValuePair<int, Item> composite, int dynamicPlaceholderId, string parentPh)
    //{
    //  XAttribute xattribute = compositeRendering.Attribute((XName)"ph");
    //  if (xattribute == null)
    //    return string.Empty;
    //  string str1 = xattribute.Value;
    //  parentPh = parentPh.StartsWith("/", StringComparison.Ordinal) ? parentPh : "/" + parentPh;
    //  string str2;
    //  if (str1.StartsWith("/", StringComparison.Ordinal))
    //  {
    //    string[] strArray = str1.Split('/');
    //    strArray[1] = string.Format("{0}-{1}-{2}", (object)strArray[1], (object)composite.Key, (object)dynamicPlaceholderId);
    //    str2 = parentPh + string.Join("/", strArray);
    //  }
    //  else
    //  {
    //    string str3 = string.Format("{0}-{1}-{2}", (object)str1, (object)composite.Key, (object)dynamicPlaceholderId);
    //    str2 = parentPh + "/" + str3;
    //  }
    //  return str2;
    //}

    //protected void SetOriginalDataSource(XElement compositeRendering)
    //{
    //  if (!compositeRendering.HasAttributes || compositeRendering.Attribute((XName)"ds") == null || compositeRendering.Attribute((XName)"ods") != null)
    //    return;
    //  string str = compositeRendering.Attribute((XName)"ds").Value;
    //  compositeRendering.Add((object)new XAttribute((XName)"ods", (object)str));
    //}

    //protected virtual void SetPartialDesignId(XElement compositeRendering, string partialDesignId)
    //{
    //  if (string.IsNullOrEmpty(partialDesignId))
    //    return;
    //  compositeRendering.Add((object)new XAttribute((XName)"sid", (object)partialDesignId));
    //}

    //protected ID IdAttribute(XElement cmps)
    //{
    //  XAttribute xattribute = cmps.Attribute((XName)"id");
    //  if (xattribute != null)
    //    return ID.Parse(xattribute.Value);
    //  Log.Error(string.Format("SXA: Could not find 'id' attribute for node {0}", (object)cmps), (object)this);
    //  return ID.Null;
    //}

    //protected string CreateCompositesXmlCacheKey(ID itemId, ID siteId)
    //{
    //  return string.Format("{0}::{1}::{2}::{3}::{4}::{5}", (object)"SXA::CompositesXml", (object)siteId, (object)this.Context.Database.Name, (object)this.Context.Device.ID, (object)this.Context.Language.Name, (object)itemId);
    //}

    //protected virtual void StoreValueInCache(string cacheKey, string value)
    //{
    //  DictionaryCache dictionaryCache = this.DictionaryCache;
    //  string id = cacheKey;
    //  DictionaryCacheValue dictionaryCacheValue = new DictionaryCacheValue();
    //  dictionaryCacheValue.Properties[(object)"CompositesXml"] = (object)value;
    //  dictionaryCache.Set(id, dictionaryCacheValue);
    //}
  }
}
