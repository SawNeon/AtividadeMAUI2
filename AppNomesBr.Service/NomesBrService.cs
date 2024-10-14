using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Entities;
using System.Text;

namespace AppNomesBr.Service
{
    public class NomesBrService : INomesBrService
    {
        private readonly INomesApi ibgeNomesApiService;
        private readonly ILogger<NomesBrService> logger;
        private readonly INomesBrRepository nomesBrRepository;

        public NomesBrService(INomesApi ibgeNomesApiService, ILogger<NomesBrService> logger, INomesBrRepository nomesBrRepository)
        {
            this.ibgeNomesApiService = ibgeNomesApiService;
            this.logger = logger;
            this.nomesBrRepository = nomesBrRepository;
        }

        public async Task<RankingNomesRoot[]> ListaTop20Nacional()
        {
            try
            {
                logger.LogInformation("Consultando top 20 nomes no Brasil");

                var result = await ibgeNomesApiService.RetornaCensosNomesRanking();
                var rankingNomesRoot = JsonSerializer.Deserialize<RankingNomesRoot[]>(result);
                if (rankingNomesRoot == null)
                    throw new InvalidDataException("Metodo: \"" + nameof(ListaTop20Nacional) + "\" a variavel \"" + nameof(rankingNomesRoot) + "\" eh nula!");

                return rankingNomesRoot;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
                return Array.Empty<RankingNomesRoot>();
            }
        }

        // Método que recebe o código da cidade (IBGE) e o sexo
        public async Task<RankingNomesRoot[]> ListaTop20PorMunicipioESexo(string codigoCidade, string sexo)
        {
            try
            {
                logger.LogInformation("Consultando top 20 nomes por município e sexo");

                var result = await ibgeNomesApiService.RetornaCensosNomesRankingFiltros(codigoCidade, sexo);
                var rankingNomesRoot = JsonSerializer.Deserialize<RankingNomesRoot[]>(result);

                if (rankingNomesRoot == null)
                    throw new InvalidDataException("Método: \"" + nameof(ListaTop20PorMunicipioESexo) + "\" a variável \"rankingNomesRoot\" é nula!");

                return rankingNomesRoot;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
                return Array.Empty<RankingNomesRoot>();
            }
        }

        // Método que recebe o nome da cidade e o sexo (renomeado para evitar conflito)
        public async Task<RankingNomesRoot[]> ListaTop20PorCidadeESexo(string cidade, string sexo)
        {
            var codigoIbge = await ObterCodigoIBGEPorCidade(cidade);
            if (string.IsNullOrEmpty(codigoIbge))
                return null;

            return await ListaTop20PorMunicipioESexo(codigoIbge, sexo);
        }

        public async Task InserirNovoRegistroNoRanking(string nome, string sexo)
        {
            try
            {
                logger.LogInformation("Inserir novo registro no ranking");

                var result = await ibgeNomesApiService.RetornaCensosNomesPeriodo(nome);
                var frequenciaPeriodo = JsonSerializer.Deserialize<NomeFrequenciaPeriodoRoot[]>(result) ?? throw new InvalidDataException("Erro ao buscar pelos dados do nome informado");
                var resultado = frequenciaPeriodo.FirstOrDefault() == null ? throw new InvalidDataException("Erro ao buscar pelos dados do nome informado") : frequenciaPeriodo.FirstOrDefault()?.Resultado;

                var novoRegistro = new NomesBr
                {
                    Nome = nome,
                    Sexo = sexo,
                    Periodo = FormataPeriodo(resultado),
                    Ranking = 1,
                    Frequencia = resultado != null ? resultado.Sum(x => x.Frequencia) : 0
                };

                List<NomesBr> antigos = await nomesBrRepository.GetAll();
                antigos.Add(novoRegistro);
                await AtualizarRanking(antigos);

                novoRegistro.Ranking = antigos[^1].Ranking;

                await nomesBrRepository.Create(novoRegistro);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
            }
        }

        public async Task<RankingNomesRoot[]> ListaMeuRanking()
        {
            var consultaTodos = await nomesBrRepository.GetAll();

            ArgumentNullException.ThrowIfNull(consultaTodos);

            var retorno = new List<RankingNomesRoot>
            {
                new RankingNomesRoot { Resultado = new List<RankingNome>() }
            };

            for (int i = 0; i < consultaTodos.Count; i++)
            {
                var novo = new RankingNome
                {
                    Frequencia = consultaTodos[i].Frequencia,
                    Nome = consultaTodos[i].Nome,
                    Ranking = consultaTodos[i].Ranking,
                    Sexo = consultaTodos[i].Sexo
                };

                retorno[0].Resultado?.Add(novo);
            }

            retorno[0].Resultado = retorno[0].Resultado?.OrderBy(x => x.Ranking).ToList();

            return retorno.ToArray();
        }

        private static string FormataPeriodo(List<FrequenciaPeriodo>? periodo)
        {
            ArgumentNullException.ThrowIfNull(periodo);

            string primeiroPeriodo = periodo[0].Periodo;
            string? ultimoPeriodo = periodo[^1].Periodo;

            if (primeiroPeriodo != ultimoPeriodo)
            {
                StringBuilder sb = new();
                if (primeiroPeriodo?[..1] == "[")
                {
                    sb.Append('[');
                    primeiroPeriodo = primeiroPeriodo.Substring(1, 4);
                    sb.Append(primeiroPeriodo);
                    sb.Append(" - ");
                    string? temp = ultimoPeriodo?.Replace("[", "]");
                    sb.Append(temp?[(temp.IndexOf(',') + 1)..]);
                }
                else
                {
                    sb.Append('[');
                    sb.Append(primeiroPeriodo?.Replace("[", " - "));
                    string? temp = ultimoPeriodo?.Replace("[", "]");
                    sb.Append(temp?[(temp.IndexOf(',') + 1)..]);
                }

                return sb.ToString();
            }

            return primeiroPeriodo;
        }

        private static List<NomesBr> OrganizarRanking(List<NomesBr> nomes)
        {
            var ordenados = nomes.OrderByDescending(x => x.Frequencia).ToList();
            for (int i = 0; i < ordenados.Count; i++)
                ordenados[i].Ranking = i + 1;

            return ordenados;
        }

        public async Task<string?> ObterCodigoIBGEPorCidade(string cidade)
        {
            try
            {
                using HttpClient client = new();
                var url = $"https://servicodados.ibge.gov.br/api/v1/localidades/municipios/{cidade}";
                var response = await client.GetStringAsync(url);

                var municipioResponse = JsonSerializer.Deserialize<MunicipioIbgeResponse>(response);
                return municipioResponse?.id.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<string?> ObterCodigoIBGEPorMunicipio(string cidade)
        {
            return await ObterCodigoIBGEPorCidade(cidade);
        }

        public async Task<RankingNomesRoot[]> ListaTop20PorMunicipioESexoComCodigo(string cidade, string sexo)
        {
            var codigoIbge = await ObterCodigoIBGEPorMunicipio(cidade);
            if (string.IsNullOrEmpty(codigoIbge))
                throw new Exception("Município não encontrado!");

            return await ListaTop20PorMunicipioESexo(codigoIbge, sexo);
        }

        private async Task AtualizarRanking(List<NomesBr> nomes)
        {
            nomes = OrganizarRanking(nomes);
            for (int i = 0; i < nomes.Count; i++)
                await nomesBrRepository.Update(nomes[i]);
        }

        public class MunicipioIbgeResponse
        {
            public int id { get; set; }
            public string nome { get; set; }
        }
    }
}
