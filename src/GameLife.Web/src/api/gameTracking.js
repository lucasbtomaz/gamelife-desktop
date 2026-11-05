async function request(url, options) {
  const response = options ? await fetch(url, options) : await fetch(url);
  const content = response.status === 204 ? null : await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(content?.detail || content?.mensagem || 'Não foi possível concluir a operação.');
  return content;
}

const jsonOptions = (method, data) => ({
  method,
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(data)
});

export const listGameTrackings = () => request('/api/acompanhamentos-jogos');
export const createGameTracking = (data) => request('/api/acompanhamentos-jogos', jsonOptions('POST', data));
export const updateGameTracking = (id, data) => request(`/api/acompanhamentos-jogos/${id}`, jsonOptions('PUT', data));
export const deleteGameTracking = (id) => request(`/api/acompanhamentos-jogos/${id}`, { method: 'DELETE' });
