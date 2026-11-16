const formatadorPercentual = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 1
});

export function limitarPercentual(valor) {
  const numero = Number(valor);
  if (!Number.isFinite(numero)) return 0;
  return Math.min(100, Math.max(0, numero));
}

export function formatarPercentual(valor) {
  return `${formatadorPercentual.format(limitarPercentual(valor))}%`;
}
