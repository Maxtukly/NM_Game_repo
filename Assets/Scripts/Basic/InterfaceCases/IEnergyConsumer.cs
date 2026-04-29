public interface IEnergyConsumer
{
    float ConsumeEnergy(float energy);
    float GetExpectedDemand(float futureTime);
}