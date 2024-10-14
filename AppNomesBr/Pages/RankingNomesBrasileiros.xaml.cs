using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.Services;
using System.Text.Json;

namespace AppNomesBr.Pages;

public partial class RankingNomesBrasileiros : ContentPage
{
    private readonly INomesBrService service;
    public RankingNomesBrasileiros(INomesBrService service)
    {
        InitializeComponent();
        this.service = service;
        BtnAtualizar.Clicked += BtnAtualizarC;
    }

    protected override async void OnAppearing()
    {
        await CarregarNomes();
        base.OnAppearing();
    }

    private async Task CarregarNomes()
    {
        var result = await service.ListaTop20Nacional();
        this.GrdNomesBr.ItemsSource = result.FirstOrDefault()?.Resultado;
    }

    private async void BtnAtualizarC(object? sender, EventArgs e)
    {
        await AtualizarNomesComFiltros();
    }

    private async Task AtualizarNomesComFiltros()
    {
        var sexo = SexoMRadioButton.IsChecked ? "M" : SexoFRadioButton.IsChecked ? "F" : null;
        var cidade = CidadeEntry.Text;

        var result = await service.ListaTop20PorMunicipioESexo(cidade, sexo);

        if (result?.FirstOrDefault()?.Resultado != null)
        {
            GrdNomesBr.ItemsSource = result.FirstOrDefault()?.Resultado;
        }
        else
        {
            await DisplayAlert("Erro", "Município não encontrado ou sem dados!", "OK");
        }
    }
}
