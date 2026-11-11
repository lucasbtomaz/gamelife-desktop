import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { vi } from 'vitest';
import GameTrackingScreen from './GameTrackingScreen.jsx';

const registro = {
  id: 1, jogoId: 10, titulo: 'Hades', plataforma: 'PC', status: 'Zerado', dataStatus: '2026-11-05T00:00:00',
  tempoEmHoras: 30, conquistasPossiveis: 20, conquistasObtidas: 10, percentualConquistas: 50, estaNaBiblioteca: false
};
const registroXbox = {
  ...registro,
  id: 2,
  plataforma: 'XBOX',
  status: 'NaoTerminado',
  tempoEmHoras: 8,
  conquistasObtidas: 4,
  percentualConquistas: 20
};

beforeEach(() => { global.fetch = vi.fn().mockResolvedValue({ ok: true, json: async () => [] }); });
afterEach(() => vi.restoreAllMocks());

test('cadastra acompanhamento com inclusão opcional na Biblioteca', async () => {
  render(<GameTrackingScreen />);
  expect(await screen.findByText(/nenhum jogo acompanhado/i)).toBeInTheDocument();
  fetch.mockResolvedValueOnce({ ok: true, json: async () => registro });

  fireEvent.change(screen.getByLabelText('Nome do jogo'), { target: { value: 'Hades' } });
  fireEvent.change(screen.getByLabelText('Plataforma'), { target: { value: 'PC' } });
  fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'Zerado' } });
  fireEvent.change(screen.getByLabelText('Conquistas possíveis'), { target: { value: '20' } });
  fireEvent.change(screen.getByLabelText('Conquistas obtidas'), { target: { value: '10' } });
  fireEvent.click(screen.getByLabelText('Adicionar à Biblioteca'));
  fireEvent.click(screen.getByRole('button', { name: 'Salvar acompanhamento' }));

  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/acompanhamentos-jogos', expect.objectContaining({
    method: 'POST', body: expect.stringContaining('"adicionarBiblioteca":true')
  })));
  expect(await screen.findByRole('heading', { name: 'Hades' })).toBeInTheDocument();
  expect(screen.getByText('50%')).toBeInTheDocument();
});

test('edita e exclui um acompanhamento existente', async () => {
  fetch.mockResolvedValueOnce({ ok: true, json: async () => [registro] });
  vi.spyOn(window, 'confirm').mockReturnValue(true);
  render(<GameTrackingScreen />);
  expect(await screen.findByRole('heading', { name: 'Hades' })).toBeInTheDocument();

  fireEvent.click(screen.getByRole('button', { name: 'Editar Hades em PC' }));
  expect(screen.getByRole('button', { name: 'Atualizar acompanhamento' })).toBeInTheDocument();
  expect(screen.getByText('Data da conclusão')).toBeInTheDocument();

  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({ ...registro, status: 'Dropado' }) });
  fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'Dropado' } });
  fireEvent.click(screen.getByRole('button', { name: 'Atualizar acompanhamento' }));
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/acompanhamentos-jogos/1', expect.objectContaining({ method: 'PUT' })));

  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({}) });
  fireEvent.click(screen.getByRole('button', { name: 'Excluir Hades em PC' }));
  await waitFor(() => expect(screen.queryByRole('heading', { name: 'Hades' })).not.toBeInTheDocument());
});

test('agrupa dois acompanhamentos do mesmo jogo em uma ficha', async () => {
  fetch.mockResolvedValueOnce({ ok: true, json: async () => [registro, registroXbox] });

  render(<GameTrackingScreen />);

  expect(await screen.findByRole('heading', { name: 'Hades' })).toBeInTheDocument();
  expect(screen.getAllByRole('article')).toHaveLength(1);
  expect(screen.getByRole('group', { name: 'PC' })).toBeInTheDocument();
  const xbox = screen.getByRole('group', { name: 'XBOX' });
  expect(xbox).toBeInTheDocument();
  expect(within(xbox).getByText('Em andamento')).toBeInTheDocument();
});

test('edita somente a plataforma selecionada dentro da ficha', async () => {
  fetch.mockResolvedValueOnce({ ok: true, json: async () => [registro, registroXbox] });
  render(<GameTrackingScreen />);
  await screen.findByRole('heading', { name: 'Hades' });

  fireEvent.click(screen.getByRole('button', { name: 'Editar Hades em XBOX' }));
  expect(screen.getByLabelText('Plataforma')).toHaveValue('XBOX');
  expect(screen.getByLabelText('Tempo em horas')).toHaveValue(8);

  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({ ...registroXbox, tempoEmHoras: 12 }) });
  fireEvent.change(screen.getByLabelText('Tempo em horas'), { target: { value: '12' } });
  fireEvent.click(screen.getByRole('button', { name: 'Atualizar acompanhamento' }));

  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/acompanhamentos-jogos/2', expect.objectContaining({ method: 'PUT' })));
  expect(screen.getByRole('group', { name: 'PC' })).toHaveTextContent('30 h');
  expect(screen.getByRole('group', { name: 'XBOX' })).toHaveTextContent('12 h');
});
