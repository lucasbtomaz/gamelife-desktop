import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';
import GameTrackingScreen from './GameTrackingScreen.jsx';

const registro = {
  id: 1, titulo: 'Hades', plataforma: 'PC', status: 'Zerado', dataStatus: '2026-11-05T00:00:00',
  tempoEmHoras: 30, conquistasPossiveis: 20, conquistasObtidas: 10, percentualConquistas: 50, estaNaBiblioteca: false
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

  fireEvent.click(screen.getByRole('button', { name: 'Editar Hades' }));
  expect(screen.getByRole('button', { name: 'Atualizar acompanhamento' })).toBeInTheDocument();
  expect(screen.getByText('Data da conclusão')).toBeInTheDocument();

  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({ ...registro, status: 'Dropado' }) });
  fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'Dropado' } });
  fireEvent.click(screen.getByRole('button', { name: 'Atualizar acompanhamento' }));
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/acompanhamentos-jogos/1', expect.objectContaining({ method: 'PUT' })));

  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({}) });
  fireEvent.click(screen.getByRole('button', { name: 'Excluir Hades' }));
  await waitFor(() => expect(screen.queryByRole('heading', { name: 'Hades' })).not.toBeInTheDocument());
});
