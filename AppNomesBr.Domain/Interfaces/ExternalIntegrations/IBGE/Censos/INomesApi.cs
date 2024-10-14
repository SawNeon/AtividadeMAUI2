namespace AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos
{
    public interface INomesApi
    {
        Task<string> RetornaCensosNomesRanking();
        Task<string> RetornaCensosNomesRankingFiltros(string cidadeEntry, string SexoOpc);
        Task<string> RetornaCensosNomesPeriodo(string nome);
        
    }
}
