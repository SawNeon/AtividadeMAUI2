using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Interfaces.Services;

namespace AppNomesBr.Pages;

public partial class NovaConsultaNome : ContentPage
{
    private readonly INomesBrService service;
    private readonly INomesBrRepository repository;

    public NovaConsultaNome(INomesBrService service, INomesBrRepository repository)
    {
        InitializeComponent();
        this.service = service;
        this.repository = repository;
        BtnPesquisar.Clicked += BtnPesquisarC;
        BtnDeleteAll.Clicked += BtnDeleteAllC;
        SexOpt();
    }

    protected override async void OnAppearing()
    {
        await CarregarNomes();
        base.OnAppearing();
    }
    private async void BtnDeleteAllC(object? sender, EventArgs e)
    {
        var registros = await repository.GetAll();

        foreach (var registro in registros)
            await repository.Delete(registro.Id);

        await CarregarNomes();
    }

    private async void BtnPesquisarC(object? sender, EventArgs e)
    {
        if (PKSX.SelectedItem == null)
        {
            await DisplayAlert("Erro", "Por favor, selecione um sexo", "OK");
            return;
        }
        var sexo = PKSX.SelectedItem?.ToString() ?? string.Empty;
        await service.InserirNovoRegistroNoRanking(TxtNome.Text.ToUpper(), sexo);
        await CarregarNomes();
       
    }

    private async Task CarregarNomes()
    {
        var result = await service.ListaMeuRanking();
        this.GrdNomesBr.ItemsSource = result.FirstOrDefault()?.Resultado;
    }

    private void SexOpt()
    {
        var sex = new List<string>
        {
        "M", "F"
        };
        PKSX.ItemsSource = sex;
    }
    private async void PickerSexoFiltro_SelectedIndexChanged(object sender, EventArgs e)
    {
        string sexo = PickerSexoFiltro.SelectedItem as string;

        if (sexo == "Todas")
        {
            await CarregarNomes();
        }
        else
        {
            await CarregarNomesPorSexo(sexo);
        }
    }

    private async Task CarregarNomesPorSexo(string sexo)
    {
        var result = await service.ListaMeuRanking();

        var nomesFiltrados = result.FirstOrDefault()?.Resultado
            .Where(n => n.Sexo == sexo)
            .ToList();

        this.GrdNomesBr.ItemsSource = nomesFiltrados;
    }

    private async void BtnRemoverFiltro(object sender, EventArgs e)
    {
        PickerSexoFiltro.SelectedItem = "Todas";
        await CarregarNomes();
    }

}