﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SoftBeckhoff.Extensions;
using SoftBeckhoff.Models;
using SoftBeckhoff.Structs;
using TwinCAT.Ads;
using TwinCAT.Ads.Server;
using TwinCAT.Ams;
using TwinCAT.TypeSystem;

namespace SoftBeckhoff.Services
{
    public class BeckhoffServer : IDisposable, IAmsFrameReceiver
    {
        private readonly ILogger logger;
        private readonly AmsServerNet server;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly MemoryObject memory = new MemoryObject();
        private uint notificationCounter = 1;

        public BeckhoffServer(ILogger logger)
        {
            this.logger = logger;

            InitializeServerMemory();
            
            server = (AmsServerNet) typeof(AmsServerNet)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, 
                    null, 
                    CallingConventions.Any, 
                    new []{typeof(ILogger)}, 
                    null)
                ?.Invoke(new[] {logger});
            logger.LogInformation($"Beckhoff server created");
            Task.Delay(500).Wait();
            var result = server.AmsConnect(851, "SoftPlc");
            var connected = server.IsServerConnected;
            logger.LogInformation($"Beckhoff server connected = {connected} with result = {result}");

            disposables.Add(server);

            server.RegisterReceiver(this);

            AddSymbol(new AdsSymbol("doNotRemove", typeof(byte)));
            AddSymbol(new AdsSymbol("PC_PLC.b_error", typeof(short))); // none 0 value 3 will fail
            //short b_error = 3;
            //WriteSymbol("PC_PLC.b_error", b_error.GetBytes());
            AddSymbol(new AdsSymbol("PC_PLC.b_Error", typeof(int))); // none 0 value 42 will fail
            int b_error = 0;
            WriteSymbol("PC_PLC.b_Error", b_error.GetBytes());
            AddSymbol(new AdsSymbol("PC_PLC.s_MoveVel", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_reset", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.b_AxisActPos", typeof(double)));
            AddSymbol(new AdsSymbol("PC_PLC.b_AxisTrigValue", typeof(double)));
            AddSymbol(new AdsSymbol("PC_PLC.b_XTemp", typeof(double)));
            AddSymbol(new AdsSymbol("PC_PLC.b_YTemp", typeof(double)));
            AddSymbol(new AdsSymbol("PC_PLC.b_ZTemp", typeof(double)));
            AddSymbol(new AdsSymbol("PC_PLC.s_MeasureSetMode", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.b_NewRapidLocate", typeof(double)));
            AddSymbol(new AdsSymbol("PC_PLC.s_calibTrigvalue", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_calibLimitvalue", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_ProbeTouchVel", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_BackOffRelPos", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_triggerWaitTime", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.b_Get_A_angle", typeof(double)));
            AddSymbol(new AdsSymbol("PC_PLC.b_Get_B_angle", typeof(double)));
            AddSymbol(new AdsSymbol("PC_PLC.s_ManualVel", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_ClockwiseZ", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_speed", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_home", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_limitvalue", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_miu1_step2", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_miu2_step2", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_R_up", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_R_down", typeof(int)));
            AddSymbol(new AdsSymbol("PC_PLC.s_dis_thresh", typeof(int)));

            AddSymbol(new AdsSymbol("PC_PLC.b_Zero_OK", typeof(bool)));
            WriteSymbol("PC_PLC.b_Zero_OK", true.GetBytes());
        }

        public Dictionary<string, AdsSymbol> Symbols { get; set; } = new Dictionary<string, AdsSymbol>();
        public Dictionary<int, IDisposable> Notifications { get; set; } = new Dictionary<int, IDisposable>();

        public byte[] ReadSymbol(string name)
        {
            if (Symbols.ContainsKey(name))
            {
                return memory.GetData(Symbols[name].Header.IndexGroup, Symbols[name].Header.IndexOffset,
                    Symbols[name].Header.Size);
            }
            else
                throw new ArgumentException("Symbol not found!");
        }
        
        public void WriteSymbol(string name, byte[] data)
        {
            if (Symbols.ContainsKey(name))
            {
                memory.SetData(Symbols[name].Header.IndexGroup, Symbols[name].Header.IndexOffset, data);
            }
            else
                throw new ArgumentException("Symbol not found!");
        }
        
        public void AddSymbol(AdsSymbol symbol)
        {
            if (Symbols.ContainsKey(symbol.Name)) throw new ArgumentException($"Symbol with the same name ('{symbol.Name}') already exists");
            
            //Define Symbol Offset
            var offset = GetCurrentDataOffset();
            symbol.Offset = offset;
            var symbolBytes = symbol.GetBytes();
            //Add symbol to list
            Symbols.Add(symbol.Name, symbol);
            //Add symbol to data
            memory.AddData(61451, symbolBytes);
            memory.AddData(61449, symbolBytes);
            //add symbol handlers
            memory.AddData(61443, offset.GetBytes());
            memory.AddData(61446, offset.GetBytes());
            //Update symbolUploadInfo
            memory.SetData(61455, new SymbolUploadInfo(Symbols.Count, GetCurrentSymbolSize()).GetBytes());
            //Add Data
            memory.AddData(61445, new byte[symbol.Size]);
        }

        private int GetCurrentDataOffset()
        {
            return memory.Count(61445);
        }

        private int GetCurrentSymbolSize()
        {
            return memory.Count(61451);
        }

        private void InitializeServerMemory()
        {
            //Set Upload Info
            memory.SetData(61455, new byte[64]);
            
            //Set Symbols for read
            memory.SetData(61451, new byte[0]);
            
            //Set symbols for readwrite
            memory.SetData(61449, new byte[0]);
            //Set symbols for readwrite Handlers
            memory.SetData(61443, new byte[0]);
            
            //cleanup
            memory.SetData(61446, new byte[1024]);
            
            //Set Datatype
            memory.SetData(61454, AdsDataTypeEntry.GetBytes());
            
            //Set Data (access over group + offset stored into handlers)
            memory.SetData(61445, new byte[0]);

        }
        

        public void Dispose()
        {
            disposables?.Dispose();
            foreach (var notification in Notifications)
            {
                notification.Value.Dispose();
            }
        }

        public byte[] RunStatus { get; set; } = {0, 0, 0, 0, 5, 0, 0, 0};
        
        public SymbolUploadInfo SymbolUploadInfo { get; set; } = new SymbolUploadInfo(0,0);
        public AdsSymbolEntry AdsSymbolEntry { get; set; } = new AdsSymbolEntry(Unit.Default);
        public AdsDataTypeEntry AdsDataTypeEntry { get; set; } = new AdsDataTypeEntry(null);
        
        public async Task<AdsErrorCode> OnReceivedAsync(AmsCommand frame, CancellationToken cancel)
        {
            logger.LogDebug($"onreceive frame: {frame.Dump()}, commandId: {frame.Header.CommandId}");
            if (frame.Header.CommandId != AdsCommandId.ReadState)
                logger.LogInformation($"{frame.Dump()}");

            var responseData = new List<byte>();
            
            if (frame.Header.CommandId == AdsCommandId.Read)
            {
                var request = frame.Data.ToArray().ByteArrayToStructure<ReadRequestData>();
                logger.LogDebug($"Data: {request}");

                var data = memory.GetData(request.IndexGroup, request.Offset, request.Length);
                var responseHeader = new ResponseHeaderData {Lenght = (uint)data.Length};
                responseData.AddRange(responseHeader.GetBytes());
                responseData.AddRange(data);
                
            }
            else if (frame.Header.CommandId == AdsCommandId.ReadDeviceInfo)
            {
                if (this.logger != null)
                    this.logger.LogDebug("OnReadDeviceInfoIndication(Addr:{0},IId:{1})", (object) frame.Header.Sender, (object) frame.Header.HUser);
                AdsVersion adsVersion = new AdsVersion(1, 1, 1);
                return await this.ReadDeviceInfoResponseAsync(frame.Header.Sender, frame.Header.HUser, AdsErrorCode.NoError, "ba_plc", adsVersion, cancel);
            }
            else if (frame.Header.CommandId == AdsCommandId.ReadState)
            {
                responseData.AddRange(RunStatus);
            }
            else if (frame.Header.CommandId == AdsCommandId.ReadWrite)
            {
                var request = frame.Data.ToArray().ByteArrayToStructure<ReadWriteRequestData>();
                logger.LogDebug($"Data: {request}");
                logger.LogDebug("Data: "+string.Join(":", frame.Data.ToArray().Skip(new ReadWriteRequestData().GetSize()).Select(b => b.ToString("X2"))));
                //Data contains Instance path encoded
                var inputData = frame.Data.ToArray().Skip(new ReadWriteRequestData().GetSize()).ToArray();
                var inputString = Encoding.ASCII.GetString(inputData).Trim('\0');
                logger.LogDebug($"inputString: {inputString}");
                var data = new byte[0];
                //inputString != "PC_PLC.s_dis_thresh" && inputString != "PC_PLC.s_R_down" && inputString != "PC_PLC.s_R_up" && inputString != "PC_PLC.s_miu2_step2" && inputString != "PC_PLC.s_miu1_step2" && inputString != "PC_PLC.s_limitvalue" && inputString != "PC_PLC.s_home" && inputString != "PC_PLC.s_speed" && inputString != "PC_PLC.s_ClockwiseZ" && inputString != "PC_PLC.s_ManualVel" && inputString != "PC_PLC.b_Zero_OK" && inputString != "PC_PLC.b_Get_B_angle" && inputString != "PC_PLC.b_Get_A_angle" && inputString != "PC_PLC.s_triggerWaitTime" && inputString != "PC_PLC.s_BackOffRelPos" && inputString != "PC_PLC.s_ProbeTouchVel" && inputString != "PC_PLC.s_calibLimitvalue" && inputString != "PC_PLC.s_calibTrigvalue" && inputString != "PC_PLC.b_error" && inputString != "PC_PLC.s_MoveVel" && inputString != "PC_PLC.s_reset" && inputString != "PC_PLC.b_AxisActPos" && inputString != "PC_PLC.b_Error" && inputString != "PC_PLC.b_AxisTrigValue" && inputString != "PC_PLC.b_XTemp" && inputString != "PC_PLC.b_YTemp" && inputString != "PC_PLC.b_ZTemp" && inputString != "PC_PLC.s_MeasureSetMode" && inputString != "PC_PLC.b_NewRapidLocate"
                switch (request.IndexGroup)
                {
                    case 61449:
                        data = Symbols[inputString].GetBytes();
                        break;
                    case 61443:
                        data = Symbols[inputString].Offset.GetBytes();
                        break;
                    default:
                        data = memory.GetData(request.IndexGroup, request.Offset, request.ReadLength);
                        break;
                }

                var responseHeader = new ResponseHeaderData {Lenght = (uint)data.Length};
                responseData.AddRange(responseHeader.GetBytes());
                responseData.AddRange(data);
                
            }
            else if (frame.Header.CommandId == AdsCommandId.Write)
            {
                var request = frame.Data.ToArray().ByteArrayToStructure<WriteRequestData>();
                logger.LogDebug($"Data: {request}");
                var data = frame.Data.ToArray().Skip(new WriteRequestData().GetSize()).ToArray();
                logger.LogDebug("Data: "+string.Join(":", data.Select(b => b.ToString("X2"))));

                memory.SetData(request.IndexGroup, request.Offset, data);
                var responseHeader = new ResponseHeader();
                responseData.AddRange(responseHeader.GetBytes());
            }
            else if (frame.Header.CommandId == AdsCommandId.AddNotification)
            {
                var request = frame.Data.ToArray().ByteArrayToStructure<NotificationRequest>();
                var handler = new Random().Next();
                CreateNotification(handler, request, frame.Header.Sender, frame.Header.Target);
                responseData.AddRange(0.GetBytes());
                responseData.AddRange(handler.GetBytes());

            }
            else if (frame.Header.CommandId == AdsCommandId.DeleteNotification)
            {
                var handler = BitConverter.ToInt32(frame.Data.ToArray());
                DeleteNorification(handler);
                responseData.AddRange(0.GetBytes());
            }   
            
            var result = await server.AmsSendAsync(
                new AmsCommand(
                    new AmsHeader(frame.Header.Sender, frame.Header.Target, frame.Header.CommandId,
                        AmsStateFlags.MaskAdsResponse, (uint) responseData.Count, 0, frame.Header.HUser),
                    new ReadOnlyMemory<byte>(responseData.ToArray())), cancel);
            
            return result;
        }

        /// <summary>Sends an ADS Read Device Info response.</summary>
        /// <param name="target">The receiver's AMS address</param>
        /// <param name="invokeId">The invoke ID provided by the receiver</param>
        /// <param name="result">The ADS error code for the response</param>
        /// <param name="name">The name of this ADS server</param>
        /// <param name="version">The version of this ADS server</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous <see cref="M:TwinCAT.Ads.Server.AdsServer.ReadDeviceInfoResponseAsync(TwinCAT.Ads.AmsAddress,System.UInt32,TwinCAT.Ads.AdsErrorCode,System.String,TwinCAT.Ads.AdsVersion,System.Threading.CancellationToken)" /> operation. The <see cref="T:System.Threading.Tasks.Task`1" /> parameter contains the <see cref="T:TwinCAT.Ads.AdsErrorCode" /> as
        /// <see cref="P:System.Threading.Tasks.Task`1.Result" />.</returns>
        protected Task<AdsErrorCode> ReadDeviceInfoResponseAsync(
          AmsAddress target,
          uint invokeId,
          AdsErrorCode result,
          string name,
          AdsVersion version,
          CancellationToken cancel)
        {
          if (target == (AmsAddress) null)
            throw new ArgumentNullException(nameof (target));
          if (version == null)
            throw new ArgumentNullException(nameof (version));
          if (string.IsNullOrEmpty(name))
            throw new ArgumentOutOfRangeException(nameof (name));
          AdsReadDeviceInfoRessponseHeader adsHeader = new AdsReadDeviceInfoRessponseHeader();
          adsHeader._result = result;
          adsHeader._majorVersion = version.Version;
          adsHeader._minorVersion = version.Revision;
          adsHeader._versionBuild = (ushort) version.Build;
          byte[] bytes = StringMarshaler.DefaultEncoding.GetBytes(name.ToCharArray(), 0, name.Length < 16 ? name.Length : 16);
          adsHeader._deviceName = bytes;
          return this.SendResponseAsync(target, invokeId, AdsCommandId.ReadDeviceInfo, (ITcAdsHeader) adsHeader, (ReadOnlyMemory<byte>) Memory<byte>.Empty, cancel);
        }

        /// <summary>Send response as an asynchronous operation.</summary>
        /// <param name="target">The r addr.</param>
        /// <param name="invokeId">The invoke identifier.</param>
        /// <param name="serviceId">The service identifier.</param>
        /// <param name="adsHeader">The ads header.</param>
        /// <param name="adsData">The ads data.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous 'SendResponse' operation. The <see cref="T:System.Threading.Tasks.Task`1" /> parameter contains the <see cref="T:TwinCAT.Ads.AdsErrorCode" /> as
        /// <see cref="P:System.Threading.Tasks.Task`1.Result" />.
        /// </returns>
        private async Task<AdsErrorCode> SendResponseAsync(
          AmsAddress target,
          uint invokeId,
          AdsCommandId serviceId,
          ITcAdsHeader adsHeader,
          ReadOnlyMemory<byte> adsData,
          CancellationToken cancel)
        {
          if (cancel.IsCancellationRequested)
            return AdsErrorCode.ClientSyncTimeOut;
          int cbData = adsHeader.MarshalSize() + adsData.Length;
          AmsHeader amsHeader = new AmsHeader(target, server.ServerAddress, serviceId, AmsStateFlags.MaskAdsResponse, (uint) cbData, 0U, invokeId);
          byte[] numArray = new byte[cbData];
          AdsHeaderMarshaller.Marshal(adsHeader, adsData.Span, numArray.AsSpan<byte>());
          if (this.logger != null)
            this.logger.LogDebug("Before Sending Response: {0}", (object) adsHeader.Dump());
          AdsErrorCode adsErrorCode = await server.AmsSendAsync(new AmsCommand(amsHeader, new ReadOnlyMemory<byte>(numArray)), cancel).ConfigureAwait(false);
          if (this.logger != null)
            this.logger.LogDebug("After Sending Response: {0}", (object) adsHeader.Dump());
          return adsErrorCode;
        }

        private void DeleteNorification(int handler)
        {
            if (Notifications.ContainsKey(handler))
            {
                Notifications[handler].Dispose();
                Notifications.Remove(handler);
            }
        }

        private void CreateNotification(int handler, NotificationRequest request, AmsAddress target, AmsAddress sender)
        {
            var cycleTime = TimeSpan.FromMilliseconds(100);
            var disposable = Observable.Timer(TimeSpan.FromMilliseconds(10), cycleTime)
                    .Select(_ => memory.GetData(request.IndexGroup, request.IndexOffset, request.Length))
                    .DistinctUntilChanged(new ByteEqualityComparer())
                    .Select(data => CreateNotificationStream(handler, data))
                    .SelectMany(data => SendNotification(target, sender, data))
                    .Subscribe()
                ;
            Notifications.Add(handler, disposable);
        }

        private async Task<Unit> SendNotification(AmsAddress target, AmsAddress sender, byte[] data)
        {
            await server.AmsSendAsync(
                new AmsCommand(
                    new AmsHeader(target, sender, AdsCommandId.Notification,
                        AmsStateFlags.MaskAdsRequest, (uint) data.Length, 0, notificationCounter),
                    new ReadOnlyMemory<byte>(data)), CancellationToken.None);
            notificationCounter++;
            return Unit.Default;
        }

        private byte[] CreateNotificationStream(int handler, byte[] data)
        {
            var stream = new AdsNotification()
            {
                Length = (uint) (default(AdsNotification).GetSize()+data.Length),
                Stamps = 1,
                AdsNotificationHeader = new AdsNotificationHeader()
                {
                    Samples = 1,
                    Timestamp = DateTime.UtcNow.ToFileTime(),
                    Sample = new AdsNotificationSample()
                    {
                        Handle = (uint) handler,
                        Size = (uint) data.Length
                    }
                }
            };
            
            var buffer = new List<byte>();
            buffer.AddRange(stream.GetBytes());
            buffer.AddRange(data);
            return buffer.ToArray();
        }

        
    }
}