import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';
import WishlistScreen from './WishlistScreen.jsx';

beforeEach(() => { global.fetch = vi.fn().mockResolvedValue({ ok: true, json: async () => [] }); });
afterEach(() => vi.restoreAllMocks());

test('carrega o estado vazio e cadastra um desejo', async () => {
  render(<WishlistScreen />);
  expect(await screen.findByText(/nenhum jogo na lista/i)).toBeInTheDocument();
  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({ id: 1, titulo: 'Hades', jaTenho: false, precosAtuais: [], historicoPrecos: [] }) });
  fireEvent.change(screen.getByLabelText('Nome do jogo desejado'), { target: { value: 'Hades' } });
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar desejo' }));
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/lista-desejos', expect.objectContaining({ method: 'POST' })));
  expect(await screen.findByRole('heading', { name: 'Hades' })).toBeInTheDocument();
});

test('mostra posse, preços e mantém formulário após erro de cotação', async () => {
  fetch.mockResolvedValueOnce({ ok: true, json: async () => [{ id: 2, titulo: 'Celeste', jaTenho: true, precoDesejado: 30, menorPrecoAtual: 25, menorPrecoHistorico: 20, precosAtuais: [{ canalId: 1, canalNome: 'STEAM', precoAtual: 25, disponivel: true }], historicoPrecos: [{ id: 1, canalNome: 'STEAM', precoAtual: 20, disponivel: true, consultadoEm: '2026-10-30T10:00:00Z' }] }] });
  render(<WishlistScreen />);
  expect(await screen.findByText('Já tenho')).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Loja de Celeste'), { target: { value: 'EPIC' } });
  fireEvent.change(screen.getByLabelText('Preço atual de Celeste'), { target: { value: '19.90' } });
  fetch.mockRejectedValueOnce(new Error('sem rede'));
  fireEvent.click(screen.getByRole('button', { name: 'Salvar preço de Celeste' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível salvar o preço');
  expect(screen.getByLabelText('Loja de Celeste')).toHaveValue('EPIC');
});
