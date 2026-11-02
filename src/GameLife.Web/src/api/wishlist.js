async function requisitar(url, options) {
  const resposta = options ? await fetch(url, options) : await fetch(url);
  const conteudo = await resposta.json().catch(() => ({}));
  if (!resposta.ok) throw new Error(conteudo.mensagem || conteudo.detail || 'Não foi possível concluir a operação.');
  return conteudo;
}

export const listarDesejos = () => requisitar('/api/lista-desejos');
export const listarCanais = () => requisitar('/api/canais');
export const criarDesejo = (dados) => requisitar('/api/lista-desejos', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(dados) });
export const adicionarPreco = (id, dados) => requisitar(`/api/lista-desejos/${id}/precos`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(dados) });
export const atualizarDesejo = (id, dados) => requisitar(`/api/lista-desejos/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(dados) });
export async function excluirDesejo(id) {
  const resposta = await fetch(`/api/lista-desejos/${id}`, { method: 'DELETE' });
  if (!resposta.ok) throw new Error('Não foi possível excluir o desejo.');
}
