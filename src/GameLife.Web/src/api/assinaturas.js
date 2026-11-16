export const SERVICOS_ASSINATURA = [
  'Game Pass',
  'PlayStation Plus',
  'Apple Arcade',
  'Play Pass'
];

export const SITUACOES_ASSINATURA = [
  'Anunciado',
  'Disponível',
  'Encerrado'
];

export const formatarBadgeAssinatura = (assinatura) =>
  `${assinatura.servico} · ${assinatura.situacao}`;

export const formatarDataAssinatura = (valor) => {
  if (!valor) return '';
  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return String(valor).slice(0, 10);
  return data.toLocaleDateString('pt-BR', { timeZone: 'UTC' });
};

async function requisitar(url, options) {
  const resposta = options ? await fetch(url, options) : await fetch(url);
  const conteudo = await resposta.json().catch(() => ({}));
  if (!resposta.ok) {
    throw new Error(conteudo.mensagem || conteudo.detail || 'Não foi possível concluir a operação de assinatura.');
  }
  return conteudo;
}

export const obterAssinaturas = ({ jogoId, titulo } = {}) => {
  const pares = [];
  if (jogoId) pares.push(`jogoId=${encodeURIComponent(String(jogoId))}`);
  if (titulo) pares.push(`titulo=${encodeURIComponent(titulo)}`);
  const query = pares.join('&');
  return requisitar(`/api/assinaturas${query ? `?${query}` : ''}`);
};

export const salvarAssinatura = (dados) =>
  requisitar('/api/assinaturas', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dados)
  });

export async function excluirAssinatura(id) {
  const resposta = await fetch(`/api/assinaturas/${id}`, { method: 'DELETE' });
  if (!resposta.ok) {
    throw new Error('Não foi possível remover a assinatura.');
  }
}
