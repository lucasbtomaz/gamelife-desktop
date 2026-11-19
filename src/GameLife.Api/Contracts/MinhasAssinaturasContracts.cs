namespace gamelife_api.Contracts;

public sealed record SalvarMinhaAssinaturaRequest(string Servico, bool Ativa, DateTime? ProximaRenovacao);
public sealed record MinhaAssinaturaResponse(string Servico, bool Ativa, DateTime? ProximaRenovacao);
