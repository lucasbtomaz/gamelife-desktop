const url = '/api/minhas-assinaturas';

async function requisitar(opcoes) {
  const resposta = opcoes ? await fetch(url, opcoes) : await fetch(url);
  const dados = await resposta.json().catch(() => null);
  if (!resposta.ok || dados == null) {
    throw new Error(dados?.mensagem || dados?.detail || 'Não foi possível acessar suas assinaturas.');
  }
  return dados;
}

export const listarMinhasAssinaturas = () => requisitar();

export const salvarMinhaAssinatura = (dados) => requisitar({
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(dados)
});
