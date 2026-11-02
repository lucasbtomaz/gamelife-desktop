import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';
import App from './App.jsx';

beforeEach(() => {
  global.fetch = vi.fn().mockResolvedValue({ ok: true, json: async () => [] });
});

afterEach(() => vi.restoreAllMocks());

test('carrega o início e permite abrir a biblioteca', async () => {
  render(<App />);
  expect(screen.getByRole('heading', { name: 'Início' })).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: /biblioteca/i }));
  expect(screen.getByRole('heading', { name: 'Biblioteca' })).toBeInTheDocument();
  expect(screen.getByLabelText('Nome do jogo')).toBeInTheDocument();
  expect(screen.getByText('Mídia física')).toBeInTheDocument();
  expect(screen.getByRole('option', { name: 'PlayStation 2' })).toBeInTheDocument();
  expect(screen.getByRole('option', { name: 'PlayStation 3' })).toBeInTheDocument();
  expect(screen.getByRole('option', { name: 'STEAM' })).toBeInTheDocument();
  expect(screen.getByRole('option', { name: 'EPIC' })).toBeInTheDocument();
  expect(screen.getByRole('option', { name: 'Amazon Luna' })).toBeInTheDocument();
  expect(screen.getByRole('option', { name: 'GOG' })).toBeInTheDocument();
  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/biblioteca'));
});

test('abre a Lista de Desejos pelo menu lateral', async () => {
  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /lista de desejos/i }));
  expect(screen.getByRole('heading', { name: 'Lista de Desejos' })).toBeInTheDocument();
  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/api/lista-desejos'));
});

test('adiciona um jogo pela API', async () => {
  fetch
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
    .mockResolvedValueOnce({ ok: true, json: async () => ({ id: '1', titulo: 'Hades', plataforma: 'PC' }) });

  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /biblioteca/i }));
  fireEvent.change(screen.getByLabelText('Nome do jogo'), { target: { value: 'Hades' } });
  fireEvent.change(screen.getByLabelText('Plataforma'), { target: { value: 'PC' } });
  fireEvent.click(screen.getByLabelText('Mídia física'));
  fireEvent.click(screen.getByRole('button', { name: /adicionar à biblioteca/i }));

  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/biblioteca', expect.objectContaining({
    method: 'POST',
    body: JSON.stringify({ titulo: 'Hades', plataforma: 'PC', midiaFisica: true })
  })));
  expect(await screen.findByText('Hades')).toBeInTheDocument();
});

test('informa quando a biblioteca não pode ser carregada', async () => {
  fetch
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
    .mockResolvedValueOnce({ ok: false, json: async () => ({}) });

  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /biblioteca/i }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'Não foi possível carregar sua biblioteca. Verifique se a API está ligada.'
  );
});
