import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { vi } from 'vitest';
import WishlistScreen from './WishlistScreen.jsx';

beforeEach(() => {
  global.fetch = vi.fn().mockResolvedValue({ ok: true, json: async () => [] });
});

afterEach(() => vi.restoreAllMocks());

test('carrega o estado vazio e cadastra um desejo', async () => {
  render(<WishlistScreen />);
  expect(await screen.findByText(/nenhum jogo na lista/i)).toBeInTheDocument();
  expect(screen.queryByText(/preço que gostaria de pagar/i)).not.toBeInTheDocument();
  expect(screen.queryByPlaceholderText(/preço desejado/i)).not.toBeInTheDocument();

  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => ({ id: 1, jogoId: 10, titulo: 'Hades', jaTenho: false, precosAtuais: [], historicoPrecos: [], assinaturas: [] })
  });

  fireEvent.change(screen.getByLabelText('Nome do jogo desejado'), { target: { value: 'Hades' } });
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar desejo' }));

  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/lista-desejos', expect.objectContaining({ method: 'POST' })));
  const chamada = fetch.mock.calls.find(([url, options]) => url === '/api/lista-desejos' && options?.method === 'POST');
  expect(JSON.parse(chamada[1].body)).toEqual({ titulo: 'Hades', precoDesejado: null, observacao: null });
  expect(await screen.findByRole('heading', { name: 'Hades' })).toBeInTheDocument();
  expect(screen.queryByLabelText('Editar preço desejado de Hades')).not.toBeInTheDocument();
  expect(screen.queryByText('Preço desejado')).not.toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Adicionar assinatura para Hades' })).toBeInTheDocument();
  expect(screen.queryByLabelText('Serviço')).not.toBeInTheDocument();
});

test('busca desejos pelo nome do jogo ou pela loja', async () => {
  const itens = [
    { id: 9, jogoId: 90, titulo: 'Pokémon Legends', precosAtuais: [], historicoPrecos: [], assinaturas: [] },
    {
      id: 10,
      jogoId: 100,
      titulo: 'Celeste',
      precosAtuais: [],
      historicoPrecos: [{ id: 20, canalId: 3, canalNome: 'GOG', precoAtual: 20, disponivel: true, consultadoEm: '2026-09-01T10:00:00Z' }],
      assinaturas: []
    }
  ];
  fetch.mockImplementation(async (url) => ({ ok: true, json: async () => url === '/api/canais' ? [] : itens }));

  render(<WishlistScreen />);
  const busca = await screen.findByLabelText('Buscar por jogo ou loja');

  fireEvent.change(busca, { target: { value: 'pokemon' } });
  expect(screen.getByRole('heading', { name: 'Pokémon Legends' })).toBeInTheDocument();
  expect(screen.queryByRole('heading', { name: 'Celeste' })).not.toBeInTheDocument();

  fireEvent.change(busca, { target: { value: 'gog' } });
  expect(screen.getByRole('heading', { name: 'Celeste' })).toBeInTheDocument();
  expect(screen.queryByRole('heading', { name: 'Pokémon Legends' })).not.toBeInTheDocument();

  fireEvent.change(busca, { target: { value: 'inexistente' } });
  expect(screen.getByText('Nenhum desejo encontrado')).toBeInTheDocument();
});

test('mostra faixas de preço e alertas acumuláveis apenas em cotações disponíveis', async () => {
  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => [{
      id: 3,
      jogoId: 30,
      titulo: 'Hades II',
      jaTenho: false,
      menorPrecoAtual: 19,
      menorPrecoHistorico: 19,
      precosAtuais: [
        { canalId: 1, canalNome: 'STEAM', precoAtual: 19, precoCheio: 100, menorPrecoHistorico: 19, disponivel: true },
        { canalId: 2, canalNome: 'GOG', precoAtual: 49, precoCheio: 100, menorPrecoHistorico: 39, disponivel: true },
        { canalId: 3, canalNome: 'EPIC', precoAtual: 9, precoCheio: 100, menorPrecoHistorico: 9, disponivel: false }
      ],
      historicoPrecos: [],
      assinaturas: []
    }]
  });

  render(<WishlistScreen />);

  const steam = await screen.findByRole('group', { name: 'Cotação STEAM' });
  expect(within(steam).getByText('81% de desconto')).toBeInTheDocument();
  expect(within(steam).getByText('80% ou mais de desconto')).toBeInTheDocument();
  expect(within(steam).getByText('Menor preço histórico')).toBeInTheDocument();
  expect(within(steam).getByText('Abaixo de R$ 20')).toBeInTheDocument();
  expect(within(steam).queryByText('Abaixo de R$ 50')).not.toBeInTheDocument();

  const gog = screen.getByRole('group', { name: 'Cotação GOG' });
  expect(within(gog).getByText('Abaixo de R$ 50')).toBeInTheDocument();
  expect(within(gog).queryByText('Abaixo de R$ 20')).not.toBeInTheDocument();

  const epic = screen.getByRole('group', { name: 'Cotação EPIC' });
  expect(within(epic).getByText('Indisponível')).toBeInTheDocument();
  expect(within(epic).queryByText(/Abaixo de R\$/)).not.toBeInTheDocument();
  expect(within(epic).queryByText('Menor preço histórico')).not.toBeInTheDocument();
});

test('mostra as lojas dos menores preços atual e histórico', async () => {
  const item = {
    id: 8,
    jogoId: 80,
    titulo: 'Forza Horizon 5',
    jaTenho: false,
    menorPrecoAtual: 99,
    menorPrecoHistorico: 59,
    precosAtuais: [
      { canalId: 1, canalNome: 'Xbox One', precoAtual: 99, disponivel: true },
      { canalId: 2, canalNome: 'Xbox Series S', precoAtual: 99, disponivel: true }
    ],
    historicoPrecos: [
      { id: 10, canalId: 1, canalNome: 'Xbox One', precoAtual: 59, disponivel: true, consultadoEm: '2026-09-01T10:00:00Z' }
    ],
    assinaturas: []
  };
  fetch.mockImplementation(async (url) => ({
    ok: true,
    json: async () => url === '/api/canais' ? [{ id: 1, nome: 'Xbox One', permitePreco: false }] : [item]
  }));

  render(<WishlistScreen />);

  expect(await screen.findByRole('heading', { name: 'Forza Horizon 5' })).toBeInTheDocument();
  expect(screen.getByLabelText('Loja do menor atual')).toHaveTextContent('Xbox One +1');
  expect(screen.getByLabelText('Loja do menor histórico')).toHaveTextContent('Xbox One');
  expect(document.querySelector('#canais-preco option[value="Xbox One"]')).not.toBeNull();
});

test('mostra posse, preços e mantém formulário após erro de cotação', async () => {
  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => [{
      id: 2,
      jogoId: 20,
      titulo: 'Celeste',
      jaTenho: true,
      precoDesejado: 30,
      menorPrecoAtual: 25,
      menorPrecoHistorico: 20,
      precosAtuais: [{ canalId: 1, canalNome: 'STEAM', precoAtual: 25, disponivel: true }],
      historicoPrecos: [{ id: 1, canalNome: 'STEAM', precoAtual: 20, disponivel: true, consultadoEm: '2026-10-30T10:00:00Z' }],
      assinaturas: []
    }]
  });

  render(<WishlistScreen />);
  expect(await screen.findByText('Já tenho')).toBeInTheDocument();

  fireEvent.change(screen.getByLabelText('Loja de Celeste'), { target: { value: 'EPIC' } });
  fireEvent.change(screen.getByLabelText('Preço atual de Celeste'), { target: { value: '19.90' } });
  fetch.mockRejectedValueOnce(new Error('sem rede'));
  fireEvent.click(screen.getByRole('button', { name: 'Salvar preço de Celeste' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível salvar o preço');
  expect(screen.getByLabelText('Loja de Celeste')).toHaveValue('EPIC');
});

test('abre o histórico como gráfico com uma linha por plataforma', async () => {
  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => [{
      id: 4,
      jogoId: 40,
      titulo: 'Slime Rancher 2',
      jaTenho: false,
      precosAtuais: [],
      historicoPrecos: [
        { id: 1, canalId: 1, canalNome: 'STEAM', precoAtual: 80.99, disponivel: true, consultadoEm: '2025-05-10T12:00:00Z' },
        { id: 2, canalId: 1, canalNome: 'STEAM', precoAtual: 65.98, disponivel: true, consultadoEm: '2026-11-12T18:15:16Z' },
        { id: 3, canalId: 2, canalNome: 'PlayStation', precoAtual: 95.94, disponivel: true, consultadoEm: '2026-11-16T13:11:58Z' }
      ],
      assinaturas: []
    }]
  });

  render(<WishlistScreen />);
  const card = (await screen.findByRole('heading', { name: 'Slime Rancher 2' })).closest('article');
  fireEvent.click(within(card).getByRole('button', { name: 'Ver histórico (3)' }));

  const grafico = within(card).getByRole('group', { name: 'Histórico de preços de Slime Rancher 2' });
  expect(within(grafico).getAllByRole('img')).toHaveLength(3);
  expect(within(card).getByLabelText('Legenda do histórico de preços')).toHaveTextContent('STEAM');
  expect(within(card).getByLabelText('Legenda do histórico de preços')).toHaveTextContent('PlayStation');
  expect(card.querySelector('.price-history')).not.toBeInTheDocument();
});

test('abre formulário de assinatura somente no modal, adiciona múltiplas, edita e remove', async () => {
  fetch
    .mockResolvedValueOnce({
      ok: true,
      json: async () => [{
        id: 3,
        jogoId: 30,
        titulo: 'Hollow Knight: Silksong',
        jaTenho: false,
        precosAtuais: [],
        historicoPrecos: [],
        assinaturas: []
      }]
    })
    .mockResolvedValueOnce({ ok: true, json: async () => [] });

  render(<WishlistScreen />);
  const card = (await screen.findByRole('heading', { name: 'Hollow Knight: Silksong' })).closest('article');
  expect(within(card).getByRole('button', { name: /adicionar assinatura/i })).toBeInTheDocument();
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(screen.queryByLabelText('Serviço')).not.toBeInTheDocument();

  // Adiciona primeira assinatura via modal
  fireEvent.click(within(card).getByRole('button', { name: /adicionar assinatura/i }));
  const modal = screen.getByRole('dialog');
  expect(within(modal).getByLabelText('Serviço')).toHaveValue('Game Pass');
  fireEvent.change(within(modal).getByLabelText('Serviço'), { target: { value: 'Game Pass' } });
  fireEvent.change(within(modal).getByLabelText('Situação'), { target: { value: 'Disponível' } });
  fireEvent.change(within(modal).getByLabelText(/Data prevista/i), { target: { value: '2026-11-10' } });

  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      id: 101,
      jogoId: 30,
      servico: 'Game Pass',
      situacao: 'Disponível',
      dataDisponibilidade: '2026-11-10T00:00:00',
      dataEncerramento: null
    })
  });

  fireEvent.click(within(modal).getByRole('button', { name: 'Salvar assinatura' }));
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/assinaturas', expect.objectContaining({ method: 'POST' })));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(await within(card).findByText('Game Pass · Disponível')).toBeInTheDocument();

  // Adiciona segunda assinatura para o mesmo jogo
  fireEvent.click(within(card).getByRole('button', { name: /adicionar assinatura/i }));
  const modal2 = screen.getByRole('dialog');
  fireEvent.change(within(modal2).getByLabelText('Serviço'), { target: { value: 'PlayStation Plus' } });
  fireEvent.change(within(modal2).getByLabelText('Situação'), { target: { value: 'Anunciado' } });

  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      id: 102,
      jogoId: 30,
      servico: 'PlayStation Plus',
      situacao: 'Anunciado',
      dataDisponibilidade: null,
      dataEncerramento: null
    })
  });

  fireEvent.click(within(modal2).getByRole('button', { name: 'Salvar assinatura' }));
  expect(await within(card).findByText('PlayStation Plus · Anunciado')).toBeInTheDocument();
  expect(within(card).getByText('Game Pass · Disponível')).toBeInTheDocument();

  // Edita a primeira assinatura
  fireEvent.click(within(card).getByRole('button', { name: 'Editar assinatura Game Pass de Hollow Knight: Silksong' }));
  const modalEdicao = screen.getByRole('dialog');
  fireEvent.change(within(modalEdicao).getByLabelText('Situação'), { target: { value: 'Encerrado' } });
  fireEvent.change(within(modalEdicao).getByLabelText(/Data de encerramento/i), { target: { value: '2026-12-01' } });

  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      id: 101,
      jogoId: 30,
      servico: 'Game Pass',
      situacao: 'Encerrado',
      dataDisponibilidade: '2026-11-10T00:00:00',
      dataEncerramento: '2026-12-01T00:00:00'
    })
  });

  fireEvent.click(within(modalEdicao).getByRole('button', { name: 'Salvar assinatura' }));
  expect(await within(card).findByText('Game Pass · Encerrado')).toBeInTheDocument();

  // Remove assinatura
  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({}) });
  fireEvent.click(within(card).getByRole('button', { name: 'Remover assinatura PlayStation Plus de Hollow Knight: Silksong' }));
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/assinaturas/102', { method: 'DELETE' }));
  expect(within(card).queryByText('PlayStation Plus · Anunciado')).not.toBeInTheDocument();
});
