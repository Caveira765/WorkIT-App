using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkIT.App.Models;

/// <summary>
/// Modelo que representa uma Vaga de TI no sistema.
/// Implementa INotifyPropertyChanged para atualização reativa instantânea na interface WPF.
/// </summary>
public class Vaga : INotifyPropertyChanged
{
    private bool _favorita;
    private bool _ehNova;

    public long Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Nivel { get; set; } = "Não identificado";
    public string DataPublicacao { get; set; } = string.Empty;
    public string DataSalvo { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;

    public bool EhNova
    {
        get => _ehNova;
        set
        {
            if (_ehNova != value)
            {
                _ehNova = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusBadge));
                OnPropertyChanged(nameof(BadgeCor));
            }
        }
    }

    public bool Favorita
    {
        get => _favorita;
        set
        {
            if (_favorita != value)
            {
                _favorita = value;
                OnPropertyChanged();
            }
        }
    }

    // Propriedades formatadas para exibição direta nos Cards
    public string LocalFormatado => string.IsNullOrWhiteSpace(Cidade) && string.IsNullOrWhiteSpace(Estado)
        ? "Remoto / Não informado"
        : $"{(string.IsNullOrWhiteSpace(Cidade) ? "Brasil" : Cidade)} / {(string.IsNullOrWhiteSpace(Estado) ? "N/A" : Estado)}";

    public string StatusBadge => EhNova ? "NOVA" : "JÁ VISTA";

    public string BadgeCor => EhNova ? "#10b981" : "#64748b"; // Verde esmeralda vs Slate

    public string NivelBadgeCor => Nivel switch
    {
        "Estágio" => "#8b5cf6",      // Roxo
        "Júnior" => "#3b82f6",       // Azul
        "Pleno" => "#06b6d4",        // Ciano
        "Sênior" => "#f59e0b",       // Laranja
        _ => "#6b7280"               // Cinza
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
