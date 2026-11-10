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
  expect(screen.getByRole('heading', { name: 'Minha Biblioteca de Jogos' })).toBeInTheDocument();
  expect(screen.getByLabelText('Nome do jogo')).toBeInTheDocument();
  expect(screen.getByLabelText('Selecionar PlayStation 2')).toBeInTheDocument();
  expect(screen.getByLabelText('Selecionar PlayStation 3')).toBeInTheDocument();
  expect(screen.getByLabelText('Selecionar STEAM')).toBeInTheDocument();
  expect(screen.getByLabelText('Selecionar EPIC')).toBeInTheDocument();
  expect(screen.getByLabelText('Selecionar Amazon Luna')).toBeInTheDocument();
  expect(screen.getByLabelText('Selecionar GOG')).toBeInTheDocument();
  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/biblioteca'));
});

test('mostra no perfil lateral o total persistido do dashboard', async () => {
  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      perfil: { nome: 'Jogador', fotoDataUrl: null, exibirJogosRecentes: true },
      resumo: { totalJogos: 18 },
      bibliotecaPorPlataforma: [],
      zeradosPorPlataforma: [],
      zeradosPorAno: [],
      jogosRecentes: [],
      conquistas: []
    })
  });

  render(<App />);

  expect(await screen.findByText('18 jogos')).toBeInTheDocument();
});

test('abre a Lista de Desejos pelo menu lateral', async () => {
  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /lista de desejos/i }));
  expect(screen.getByRole('heading', { name: 'Lista de Desejos' })).toBeInTheDocument();
  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/api/lista-desejos'));
});

test('usa ícones SVG consistentes em todas as abas do menu', () => {
  render(<App />);
  const navigation = screen.getByRole('navigation', { name: 'Menu principal' });
  const buttons = Array.from(navigation.querySelectorAll('button'));

  expect(buttons).toHaveLength(7);
  buttons.forEach((button) => expect(button.querySelector('svg')).not.toBeNull());
  expect(navigation.textContent).not.toMatch(/[⌂♡✓🎧▦⚙]/u);
});

test('adiciona um jogo pela API', async () => {
  fetch
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
    .mockResolvedValueOnce({ ok: true, json: async () => ({ total: 0, zerados: 0, dropados: 0, naoTerminados: 0 }) })
    .mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        jogoId: 1,
        titulo: 'Hades',
        anoLancamento: null,
        plataformas: [{ posseId: 1, canalId: 1, nome: 'PC', midiaFisica: true, acompanhamento: null }]
      })
    });

  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /biblioteca/i }));
  fireEvent.change(screen.getByLabelText('Nome do jogo'), { target: { value: 'Hades' } });
  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Mídia física em PC'));
  fireEvent.click(screen.getByRole('button', { name: /adicionar à biblioteca/i }));

  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/biblioteca', expect.objectContaining({
    method: 'POST',
    body: JSON.stringify({
      titulo: 'Hades',
      plataformas: [{ plataforma: 'PC', midiaFisica: true }]
    })
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
