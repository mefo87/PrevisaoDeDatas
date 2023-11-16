using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

async Task ComecarData()
{
    var dataHoje = DateTime.Now;
    var listaFeriados = new ListaFeriados();
    Console.WriteLine("----- Previsão de Datas -----");
    Console.WriteLine();
    Console.WriteLine($"Data de hoje: {dataHoje.ToString("dd/MM/yyyy HH:mm:ss")}");
    Console.WriteLine($"Hoje é {dataHoje.ToString("dddd", new CultureInfo("rj-RJ"))}");
    var estado = string.Empty;
    string mensagemErro = null;
    while (mensagemErro != string.Empty)
    {
        Console.WriteLine($"Digite seu Estado! (Sigla)");
        estado = Console.ReadLine();
        mensagemErro = ValidarEntrada(estado, ETipoEntrada.Estado);

        if (mensagemErro != string.Empty)
            Console.WriteLine($"{mensagemErro}");
    }

    await listaFeriados.PopularFeriados(dataHoje.Year.ToString(), estado);
    var hjEhFeriado = listaFeriados.VerificarFeriado(dataHoje);

    if (hjEhFeriado)
    {
        Console.WriteLine();
        var feriado = listaFeriados.RecuperarFeriadoPorData(dataHoje);
        Console.WriteLine($"Hoje é Feriado de: {feriado.Name}");
        Console.WriteLine($"O feriado é a nivel: {feriado.Level}");
        Console.WriteLine($"O feriado é do tipo: {feriado.Type}");
    }

    if (!hjEhFeriado)
    {
        Console.WriteLine();
        var feriado = listaFeriados.RecuperarFeriadoMaisProximo(dataHoje);
        Console.WriteLine($"O Feriado mais proximo é de: {feriado.Name}");
        Console.WriteLine($"Sera na data: {feriado.Date.ToString("dd/MM/yyyy")}");
        Console.WriteLine($"O feriado é a nivel: {feriado.Level}");
        Console.WriteLine($"O feriado é do tipo: {feriado.Type}");
    }

    Console.WriteLine($"");

    mensagemErro = null;
    var quantDias = string.Empty;
    while (mensagemErro != string.Empty)
    {
        Console.WriteLine("Daqui a quantos dias deseja prever?");
        quantDias = Console.ReadLine();
        mensagemErro = ValidarEntrada(quantDias, ETipoEntrada.Dias);

        if (mensagemErro != string.Empty)
            Console.WriteLine($"{mensagemErro}");
    }

    var diaConvertido = int.Parse(quantDias);
    var dataPrevista = dataHoje.AddDays(diaConvertido);

    Console.WriteLine($"O dia escolhido é {dataPrevista.ToString("dd/MM/yyyy")}");
    Console.WriteLine($"O dia da semana será: {dataPrevista.ToString("dddd", new CultureInfo("rj-RJ"))}");

    var diaPrevistoEhFeriado = listaFeriados.VerificarFeriado(dataPrevista);
    await listaFeriados.PopularFeriados(dataPrevista.Year.ToString(), estado);

    if (diaPrevistoEhFeriado)
    {
        Console.WriteLine();
        var feriado = listaFeriados.RecuperarFeriadoPorData(dataPrevista);
        Console.WriteLine($"A data prevista é Feriado de: {feriado.Name}");
        Console.WriteLine($"O feriado é a nivel: {feriado.Level}");
        Console.WriteLine($"O feriado é do tipo: {feriado.Type}");
    }

    if (!diaPrevistoEhFeriado)
    {
        Console.WriteLine();
        var feriado = listaFeriados.RecuperarFeriadoMaisProximo(dataPrevista);
        Console.WriteLine($"O Feriado mais proximo é de: {feriado.Name}");
        Console.WriteLine($"Sera na data: {feriado.Date.ToString("dd/MM/yyyy")}");
        Console.WriteLine($"O feriado é a nivel: {feriado.Level}");
        Console.WriteLine($"O feriado é do tipo: {feriado.Type}");
    }

    Console.ReadLine();
}

await ComecarData();
string ValidarEntrada(string entrada, ETipoEntrada tipoEntrada)
{
    var regexNumero = new Regex("[0-9]+");

    if (string.IsNullOrWhiteSpace(entrada))
    {
        switch (tipoEntrada)
        {
            case ETipoEntrada.Estado:
                return "O estado não pode estar vazio!";
            case ETipoEntrada.Dias:
                return "A quantidade de dias não pode estar vazia!";
            default:
                return "Erro ao validar entrada de dados!";
        }
    }

    if (regexNumero.IsMatch(entrada) && tipoEntrada == ETipoEntrada.Estado)
        return "O estado deve conter apenas letras!";

    if (entrada.Length != 2 && tipoEntrada == ETipoEntrada.Estado)
        return "O estado deve ser uma sigla de apenas duas letras!";

    if (!regexNumero.IsMatch(entrada) && tipoEntrada == ETipoEntrada.Dias)
        return "A quantidade de dias precisa ser um número!";

    return string.Empty;
}


public class Feriado
{
    public DateTime Date { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Level { get; set; }
}

public class ListaFeriados
{
    public List<Feriado> Feriados { get; set; }

    public async Task PopularFeriados(string ano, string estado = "")
    {
        var httpClient = HttpClientHelper.MontarHttpClient();
        var apiUrl = $"{HttpClientHelper.ApiUrl}{ano}";

        if (!string.IsNullOrWhiteSpace(estado) && estado.Count() == 2)
            apiUrl += $"?state={estado}";

        var httpResponse = await httpClient.GetAsync(apiUrl);

        if (httpResponse.IsSuccessStatusCode)
        {
            var responseBodyString = await httpResponse.Content.ReadAsStringAsync();
            var listaFeriados = JsonConvert.DeserializeObject<List<Feriado>>(responseBodyString);

            if (listaFeriados != null)
                Feriados = listaFeriados;
        }
        else
        {
            Console.WriteLine(httpResponse.StatusCode.ToString());
        }

    }

    public bool VerificarFeriado(DateTime data)
    {
        var dataLimpa = new DateTime(data.Year, data.Month, data.Day);
        return Feriados.Any(feriado => feriado.Date == dataLimpa);
    }

    public Feriado RecuperarFeriadoPorData(DateTime data)
    {
        var dataLimpa = new DateTime(data.Year, data.Month, data.Day);
        var feriado = Feriados.FirstOrDefault(feriado => feriado.Date == dataLimpa);

        if (feriado != null)
            return feriado;

        return null;
    }

    public Feriado RecuperarFeriadoMaisProximo(DateTime data)
    {
        var dataLimpa = new DateTime(data.Year, data.Month, data.Day);
        var listaOrdernada = Feriados.OrderByDescending(feriado => feriado.Date).ToList();
        var feriadoMaisProximo = listaOrdernada.LastOrDefault(feriado => feriado.Date >= dataLimpa);

        return feriadoMaisProximo;
    }
}

public static class HttpClientHelper
{
    public static string Token { get; } = "5328|REtDdmuaQNaU8nJZYnmjBpY7lxBWnbkF";
    public static string ApiUrl { get; } = "https://api.invertexto.com/v1/holidays/";

    public static HttpClient MontarHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        return httpClient;
    }
}

public enum ETipoEntrada
{
    Estado = 0,
    Dias = 1
}



