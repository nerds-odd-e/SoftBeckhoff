// Decompiled with JetBrains decompiler
// Type: TwinCAT.Ads.Server.AdsHeaderMarshaller
// Assembly: TwinCAT.Ads.Server, Version=5.0.0.0, Culture=neutral, PublicKeyToken=180016cd49e5e8c3
// MVID: EF357D58-A8C0-49D6-B824-8DAD4D8366AE
// Assembly location: C:\Users\aspiringbuoy\.nuget\packages\beckhoff.twincat.ads.server\5.0.1-preview.11\lib\netcoreapp3.1\TwinCAT.Ads.Server.dll
// XML documentation location: C:\Users\aspiringbuoy\.nuget\packages\beckhoff.twincat.ads.server\5.0.1-preview.11\lib\netcoreapp3.1\TwinCAT.Ads.Server.xml

using System;

#nullable enable
namespace SoftBeckhoff.Services
{
  public static class AdsHeaderMarshaller
  {
    /// <summary>
    /// Marshals specified AdsHeader + Ads payload into a memory location.
    /// </summary>
    /// <param name="adsHeader">The ADS Header</param>
    /// <param name="adsData">The ADS Data payload.</param>
    /// <param name="destination">The destination memory (Full ADS Frame)</param>
    /// <returns>System.Byte[].</returns>
    internal static int Marshal(
      ITcAdsHeader adsHeader,
      ReadOnlySpan<byte> adsData,
      Span<byte> destination)
    {
      int num = 0;
      if (adsHeader != null)
      {
        num = adsHeader.MarshalSize();
        adsHeader.Marshal(destination.Slice(0, num));
        adsData.CopyTo(destination.Slice(num));
      }
      return num + adsData.Length;
    }
  }
}
