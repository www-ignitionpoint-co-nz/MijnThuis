﻿using Djohnnie.SolarEdge.ModBus.TCP;
using Djohnnie.SolarEdge.ModBus.TCP.Constants;
using Djohnnie.SolarEdge.ModBus.TCP.Types;
using Microsoft.Extensions.Configuration;

namespace MijnThuis.Integrations.Solar;

public interface IModbusService
{
    Task<SolarOverview> GetOverview();

    Task<BatteryLevel> GetBatteryLevel();

    Task<EnergyProduced> GetEnergy();

    Task<EnergyOverview> GetEnergyToday();

    Task<EnergyOverview> GetEnergyThisMonth();

    Task<StorageData> GetStorageData(StorageDataRange range);
}
internal class ModbusService : BaseService, IModbusService
{
    private readonly string _modbusAddress;
    private readonly string _modbusPort;

    public ModbusService(IConfiguration configuration) : base(configuration)
    {
        _modbusAddress = configuration.GetValue<string>("MODBUS_ADDRESS");
        _modbusPort = configuration.GetValue<string>("MODBUS_PORT");
    }

    public Task<SolarOverview> GetOverview()
    {
        throw new NotImplementedException();
    }

    public async Task<BatteryLevel> GetBatteryLevel()
    {
        using var modbusClient = new ModbusClient("192.168.10.201", 1502);
        await modbusClient.Connect();

        var soe = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Energy);
        var soh = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Health);

        modbusClient.Disconnect();

        return new BatteryLevel
        {
            Level = Convert.ToDecimal(soe.Value),
            Health = Convert.ToDecimal(soh.Value)
        };
    }

    public Task<EnergyProduced> GetEnergy()
    {
        throw new NotImplementedException();
    }

    public Task<EnergyOverview> GetEnergyToday()
    {
        throw new NotImplementedException();
    }

    public Task<EnergyOverview> GetEnergyThisMonth()
    {
        throw new NotImplementedException();
    }

    public Task<StorageData> GetStorageData(StorageDataRange range)
    {
        throw new NotImplementedException();
    }
}