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
  expect(screen.getByRole('button', { name: 'Adicionar jogo' })).toBeInTheDocument();
  expect(screen.queryByLabelText('Nome do jogo')).not.toBeInTheDocument();
  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/biblioteca'));
});

test('mantém totais e adicionar acima da grade e abre cadastro em tela exclusiva', async () => {
  fetch
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
    .mockResolvedValueOnce({ ok: true, json: async () => ({ total: 32, zerados: 2, dropados: 0, naoTerminados: 30 }) });

  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /biblioteca/i }));

  const summary = await screen.findByRole('region', { name: 'Resumo do progresso dos jogos' });
  const library = screen.getByRole('region', { name: 'Biblioteca de jogos' });
  const add = screen.getByRole('button', { name: 'Adicionar jogo' });
  expect(summary.compareDocumentPosition(library) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(add.compareDocumentPosition(library) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

  fireEvent.click(add);
  expect(screen.getByRole('heading', { name: 'Adicionar jogo' })).toBeInTheDocument();
  expect(screen.queryByRole('region', { name: 'Biblioteca de jogos' })).not.toBeInTheDocument();
  expect(screen.getByLabelText('Nome do jogo')).toBeInTheDocument();

  fireEvent.click(screen.getByRole('button', { name: 'Voltar para a biblioteca' }));
  expect(screen.getByRole('heading', { name: 'Minha Biblioteca de Jogos' })).toBeInTheDocument();
  expect(screen.queryByLabelText('Nome do jogo')).not.toBeInTheDocument();
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

test('adiciona um jogo pela tela exclusiva', async () => {
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
        plataformas: [{ posseId: 1, canalId: 1, nome: 'PC', compraDigital: true, midiaFisica: true, porAssinatura: false, acompanhamento: null }], assinaturas: []
      })
    })
    .mockResolvedValueOnce({ ok: true, json: async () => ({ total: 1, zerados: 0, dropados: 0, naoTerminados: 1 }) })
    .mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        perfil: { nome: 'Jogador', fotoDataUrl: null, exibirJogosRecentes: true },
        resumo: { totalJogos: 1 }
      })
    });

  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /biblioteca/i }));
  fireEvent.click(await screen.findByRole('button', { name: 'Adicionar jogo' }));
  fireEvent.change(screen.getByLabelText('Nome do jogo'), { target: { value: 'Hades' } });
  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Mídia física em PC'));
  fireEvent.click(screen.getByRole('button', { name: /adicionar à biblioteca/i }));

  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/biblioteca', expect.objectContaining({
    method: 'POST',
    body: JSON.stringify({
      titulo: 'Hades',
      plataformas: [{ plataforma: 'PC', compraDigital: true, midiaFisica: true, porAssinatura: false }]
    })
  })));
  expect(await screen.findByText('Hades')).toBeInTheDocument();
  await waitFor(() => {
    expect(fetch.mock.calls.filter(([url]) => url === '/api/acompanhamentos-jogos/resumo')).toHaveLength(2);
    expect(fetch.mock.calls.filter(([url]) => url === '/api/dashboard/inicio')).toHaveLength(2);
  });
  expect(screen.getByRole('region', { name: 'Resumo do progresso dos jogos' }).querySelector('.summary-total strong')).toHaveTextContent('1');
  expect(screen.getByText('1 jogo')).toBeInTheDocument();
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

test('edita uma ficha existente e adiciona outra plataforma sem duplicar o jogo', async () => {
  const game = {
    jogoId: 34,
    titulo: 'Graveyard Keeper',
    anoLancamento: 2018,
    plataformas: [{ posseId: 1, canalId: 1, nome: 'STEAM', compraDigital: true, midiaFisica: false, porAssinatura: false, acompanhamento: null }], assinaturas: []
  };
  const updated = {
    ...game,
    plataformas: [...game.plataformas, { posseId: 2, canalId: 2, nome: 'XBOX', compraDigital: true, midiaFisica: true, porAssinatura: false, acompanhamento: null }]
  };
  fetch
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
    .mockResolvedValueOnce({ ok: true, json: async () => [game] })
    .mockResolvedValueOnce({ ok: true, json: async () => ({ total: 1, zerados: 0, dropados: 0, naoTerminados: 1 }) })
    .mockResolvedValueOnce({ ok: true, json: async () => updated });

  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /biblioteca/i }));
  expect(await screen.findByRole('heading', { name: 'Graveyard Keeper' })).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Editar Graveyard Keeper' }));

  expect(screen.getByRole('heading', { name: 'Editar Graveyard Keeper' })).toBeInTheDocument();
  expect(screen.queryByRole('heading', { name: 'Minha Biblioteca de Jogos' })).not.toBeInTheDocument();
  expect(screen.queryByRole('searchbox', { name: 'Buscar jogos' })).not.toBeInTheDocument();
  expect(screen.getByLabelText('Nome do jogo')).toHaveValue('Graveyard Keeper');
  expect(screen.getByLabelText('Selecionar STEAM')).toBeChecked();
  fireEvent.click(screen.getByLabelText('Selecionar XBOX'));
  fireEvent.click(screen.getByLabelText('Mídia física em XBOX'));
  fireEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }));

  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/biblioteca', expect.objectContaining({
    method: 'POST',
    body: JSON.stringify({
      titulo: 'Graveyard Keeper',
      anoLancamento: 2018,
      plataformas: [
        { plataforma: 'STEAM', compraDigital: true, midiaFisica: false, porAssinatura: false },
        { plataforma: 'XBOX', compraDigital: true, midiaFisica: true, porAssinatura: false }
      ]
    })
  })));
  expect(screen.getByRole('heading', { name: 'Minha Biblioteca de Jogos' })).toBeInTheDocument();
  expect(screen.getAllByRole('heading', { name: 'Graveyard Keeper' })).toHaveLength(1);
  expect(screen.getByRole('group', { name: 'XBOX' })).toBeInTheDocument();
});


test('permite remover ou continuar monitorando desejo a partir do alerta na tela inicial', async () => {
  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      perfil: { nome: 'Jogador', fotoDataUrl: null, exibirJogosRecentes: true },
      resumo: { totalJogos: 1 },
      bibliotecaPorPlataforma: [],
      zeradosPorPlataforma: [],
      zeradosPorAno: [],
      jogosRecentes: [],
      conquistas: [],
      alertasListaDesejos: [
        { desejoId: 15, jogoId: 4, titulo: 'Celeste', lojasMonitoradas: ['STEAM'] }
      ]
    })
  });

  render(<App />);
  const alerta = await screen.findByRole('region', { name: 'Alertas da Lista de Desejos' });
  expect(alerta).toHaveTextContent('Celeste');
  expect(alerta).toHaveTextContent('STEAM');

  fetch
    .mockResolvedValueOnce({ ok: true, json: async () => ({}) })
    .mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        perfil: { nome: 'Jogador', fotoDataUrl: null, exibirJogosRecentes: true },
        resumo: { totalJogos: 1 },
        alertasListaDesejos: []
      })
    });

  fireEvent.click(screen.getByRole('button', { name: 'Continuar monitorando' }));
  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/api/lista-desejos/15/continuar-monitorando', { method: 'POST' }));
  await waitFor(() => expect(screen.queryByRole('region', { name: 'Alertas da Lista de Desejos' })).not.toBeInTheDocument());
});

test('abre Minhas assinaturas nas Configurações', async () => {
  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /configurações/i }));
  expect(screen.getByRole('heading', { name: 'Minhas assinaturas' })).toBeInTheDocument();
  await waitFor(() => expect(fetch).toHaveBeenCalledWith('/api/minhas-assinaturas'));
});

test('o cabeçalho conta as posses da biblioteca e confirma o cadastro de catálogo', async () => {
  const compra = { jogoId: 1, titulo: 'Comprado', plataformas: [{ posseId: 1, nome: 'PC', compraDigital: true }] };
  const catalogo = { jogoId: 2, titulo: 'Catálogo existente', plataformas: [{ posseId: 2, nome: 'PC', porAssinatura: true }] };
  fetch.mockImplementation(async (url, options) => ({
    ok: true,
    json: async () => {
      if (url === '/biblioteca' && options?.method === 'POST')
        return { ...catalogo, jogoId: 3, titulo: 'Novo catálogo' };
      if (url === '/biblioteca') return [compra, catalogo];
      return [];
    }
  }));
  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: /biblioteca/i }));
  expect(await screen.findByText('jogo na coleção')).toBeInTheDocument();
  expect(screen.queryByText('Catálogo existente')).not.toBeInTheDocument();

  fireEvent.click(screen.getByRole('button', { name: 'Adicionar jogo' }));
  fireEvent.change(screen.getByLabelText('Nome do jogo'), { target: { value: 'Novo catálogo' } });
  fireEvent.click(screen.getByLabelText('Selecionar PC'));
  fireEvent.click(screen.getByLabelText('Compra digital em PC'));
  fireEvent.click(screen.getByLabelText('Assinatura em PC'));
  fireEvent.click(screen.getByRole('button', { name: /adicionar à biblioteca/i }));

  expect(await screen.findByText('Novo catálogo foi salvo como jogo de catálogo.')).toBeInTheDocument();
  expect(screen.getByText('jogo na coleção')).toBeInTheDocument();
});
