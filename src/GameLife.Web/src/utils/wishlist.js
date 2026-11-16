export const normalizarBusca = (texto) => String(texto ?? '')
  .normalize('NFD')
  .replace(/[\u0300-\u036f]/g, '')
  .toLocaleLowerCase('pt-BR')
  .trim();

export const filtrarDesejos = (itens = [], termo = '') => {
  const busca = normalizarBusca(termo);
  if (!busca) return itens;

  return itens.filter((item) => {
    const lojasAtuais = (item.precosAtuais ?? []).map((preco) => preco.canalNome);
    const lojasHistoricas = (item.historicoPrecos ?? []).map((preco) => preco.canalNome);
    return [item.titulo, ...lojasAtuais, ...lojasHistoricas]
      .some((texto) => normalizarBusca(texto).includes(busca));
  });
};

const obterNomes = (precos, menorPreco) => {
  const menor = Number(menorPreco);
  if (!Number.isFinite(menor)) return [];

  return Array.from(new Set((precos ?? [])
    .filter((preco) => preco.disponivel && Number(preco.precoAtual) === menor)
    .map((preco) => preco.canalNome?.trim())
    .filter(Boolean)))
    .sort((a, b) => a.localeCompare(b, 'pt-BR'));
};

export const obterOrigensMenores = (item = {}) => ({
  atual: obterNomes(item.precosAtuais, item.menorPrecoAtual),
  historico: obterNomes(item.historicoPrecos, item.menorPrecoHistorico)
});

export const formatarOrigens = (nomes = []) => {
  if (nomes.length === 0) return '';
  if (nomes.length === 1) return nomes[0];
  return `${nomes[0]} +${nomes.length - 1}`;
};
