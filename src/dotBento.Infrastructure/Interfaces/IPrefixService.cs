namespace dotBento.Infrastructure.Interfaces;

public interface IPrefixService
{
    void StorePrefix(string prefix, ulong key);

    string GetPrefix(ulong? key);

    void RemovePrefix(ulong key);

    Task LoadAllPrefixes();

    Task ReloadPrefix(ulong discordGuildId);
}