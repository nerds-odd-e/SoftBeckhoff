// Decompiled with JetBrains decompiler
// Type: TwinCAT.Ads.Server.AdsReadDeviceInfoRessponseHeader
// Assembly: TwinCAT.Ads.Server, Version=5.0.0.0, Culture=neutral, PublicKeyToken=180016cd49e5e8c3
// MVID: EF357D58-A8C0-49D6-B824-8DAD4D8366AE
// Assembly location: C:\Users\aspiringbuoy\.nuget\packages\beckhoff.twincat.ads.server\5.0.1-preview.11\lib\netcoreapp3.1\TwinCAT.Ads.Server.dll
// XML documentation location: C:\Users\aspiringbuoy\.nuget\packages\beckhoff.twincat.ads.server\5.0.1-preview.11\lib\netcoreapp3.1\TwinCAT.Ads.Server.xml

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using TwinCAT.Ads;
using TwinCAT.Ads.Server;

#nullable enable
namespace SoftBeckhoff.Services
{
  /// <summary>ReadDeviceInfo Response header.</summary>
  /// <seealso cref="T:TwinCAT.Ads.Server.ITcAdsHeader" />
  public class AdsReadDeviceInfoRessponseHeader : ITcAdsHeader
  {
    internal AdsErrorCode _result;
    internal byte _majorVersion;
    internal byte _minorVersion;
    internal ushort _versionBuild;
    [MarshalAs((UnmanagedType) 30, SizeConst = 16)]
    internal byte[] _deviceName = new byte[16];

    public void Marshal(BinaryWriter writer)
    {
      writer.Write((uint) this._result);
      writer.Write(this._majorVersion);
      writer.Write(this._minorVersion);
      writer.Write(this._versionBuild);
      writer.Write(this._deviceName);
    }

    public int Marshal(Span<byte> destination)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(0, 4), (uint) this._result);
      destination[4] = this._majorVersion;
      destination[5] = this._minorVersion;
      BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(6, 2), this._versionBuild);
      this._deviceName.CopyTo<byte>(destination.Slice(8, 16));
      return this.MarshalSize();
    }

    public int MarshalSize() => 24;

    public string Dump()
    {
      return string.Format("ReadDeviceInfoResHeader(Result:{0},Major:{1},Minor:{2},Version:{3},Device:{4})", (object) this._result, (object) this._majorVersion, (object) this._minorVersion, (object) this._versionBuild, (object) this._deviceName);
    }
  }
}
