// Decompiled with JetBrains decompiler
// Type: TwinCAT.Ads.Server.ITcAdsHeader
// Assembly: TwinCAT.Ads.Server, Version=5.0.0.0, Culture=neutral, PublicKeyToken=180016cd49e5e8c3
// MVID: EF357D58-A8C0-49D6-B824-8DAD4D8366AE
// Assembly location: C:\Users\aspiringbuoy\.nuget\packages\beckhoff.twincat.ads.server\5.0.1-preview.11\lib\netcoreapp3.1\TwinCAT.Ads.Server.dll
// XML documentation location: C:\Users\aspiringbuoy\.nuget\packages\beckhoff.twincat.ads.server\5.0.1-preview.11\lib\netcoreapp3.1\TwinCAT.Ads.Server.xml

using System;

#nullable enable
namespace SoftBeckhoff.Services
{
  /// <summary>Interface for a ADS Header object.</summary>
  public interface ITcAdsHeader
  {
    /// <summary>
    /// Marshals the Header (without data) to specified memory location.
    /// </summary>
    /// <param name="destination">The writer.</param>
    int Marshal(Span<byte> destination);

    /// <summary>Gets the Marshal Size of the Header in bytes.</summary>
    /// <returns>System.Int32.</returns>
    int MarshalSize();

    /// <summary>Dumps the Header (only for internal debug purposes)</summary>
    /// <returns>System.String.</returns>
    string Dump();
  }
}
