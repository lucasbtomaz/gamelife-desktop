import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { vi } from 'vitest';
import WishlistScreen, { classificarCotacao } from './WishlistScreen.jsx';

beforeEach(() => { global.fetch = vi.fn().mockResolvedValue({ ok: true, json: async () => [] }); });
afterEach(() => vi.restoreAllMocks());

test('não arredonda desconto menor que oitenta para o selo de oportunidade', () => {
  expect(classificarCotacao({
    precoAtual: 20.1,
    precoCheio: 100,
    menorPrecoHistorico: 10,
    disponivel: true
  })).not.toContain('desconto-80');
});

test('carrega o estado vazio e cadastra um desejo', async () => {
  render(<WishlistScreen />);
  expect(await screen.findByText(/nenhum jogo na lista/i)).toBeInTheDocument();
  expect(screen.queryByText(/preço que gostaria de pagar/i)).not.toBeInTheDocument();
  expect(screen.queryByPlaceholderText(/preço desejado/i)).not.toBeInTheDocument();
  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({ id: 1, titulo: 'Hades', jaTenho: false, precosAtuais: [], historicoPrecos: [] }) });
  fireEvent.change(screen.getByLabelText('Nome do jogo desejado'), { target: { value: 'Hades' } });
  fireEvent.change(screen.getByLabelText('Por que ou quando quero jogar'), { target: { value: 'Promoção futura' } });
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar desejo' }));
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/lista-desejos', expect.objectContaining({ method: 'POST' })));
  const chamada = fetch.mock.calls.find(([url, options]) => url === '/api/lista-desejos' && options?.method === 'POST');
  expect(JSON.parse(chamada[1].body)).toEqual({
    titulo: 'Hades',
    precoDesejado: null,
    observacao: 'Promoção futura'
  });
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

test('preserva preço desejado legado ao editar somente a observação', async () => {
  const item = { id: 5, titulo: 'Neva', jaTenho: false, precoDesejado: 30, observacao: 'Antiga', precosAtuais: [], historicoPrecos: [] };
  fetch.mockResolvedValueOnce({ ok: true, json: async () => [item] });
  render(<WishlistScreen />);
  fireEvent.change(await screen.findByLabelText('Editar observação de Neva'), { target: { value: 'Nova observação' } });
  fetch.mockResolvedValueOnce({ ok: true, json: async () => ({ ...item, observacao: 'Nova observação' }) });
  fireEvent.click(screen.getByRole('button', { name: 'Atualizar detalhes' }));

  await waitFor(() => {
    const chamada = fetch.mock.calls.find(([url, options]) => url === '/api/lista-desejos/5' && options?.method === 'PUT');
    expect(JSON.parse(chamada[1].body)).toEqual({ precoDesejado: 30, observacao: 'Nova observação' });
  });
});

test('mostra alertas acumuláveis somente em cotações disponíveis', async () => {
  fetch.mockResolvedValueOnce({ ok: true, json: async () => [{
    id: 3,
    titulo: 'Hades II',
    jaTenho: false,
    menorPrecoAtual: 19,
    menorPrecoHistorico: 19,
    precosAtuais: [
      { canalId: 1, canalNome: 'STEAM', precoAtual: 19, precoCheio: 100, menorPrecoHistorico: 19, disponivel: true },
      { canalId: 2, canalNome: 'GOG', precoAtual: 9, precoCheio: 100, menorPrecoHistorico: 9, disponivel: false }
    ],
    historicoPrecos: []
  }] });

  render(<WishlistScreen />);

  const steam = await screen.findByRole('group', { name: 'Cotação STEAM' });
  expect(within(steam).getByText('81% de desconto')).toBeInTheDocument();
  expect(within(steam).getByText('80% ou mais de desconto')).toBeInTheDocument();
  expect(within(steam).getByText('Menor preço histórico')).toBeInTheDocument();
  expect(within(steam).getByText('Abaixo de R$ 20')).toBeInTheDocument();

  const gog = screen.getByRole('group', { name: 'Cotação GOG' });
  expect(within(gog).getByText('Indisponível')).toBeInTheDocument();
  expect(within(gog).queryByText('80% ou mais de desconto')).not.toBeInTheDocument();
  expect(within(gog).queryByText('Menor preço histórico')).not.toBeInTheDocument();
  expect(within(gog).queryByText('Abaixo de R$ 20')).not.toBeInTheDocument();
});

test('agrupa todo o histórico por loja e ordena do mais recente para o mais antigo', async () => {
  fetch.mockResolvedValueOnce({ ok: true, json: async () => [{
    id: 4,
    titulo: 'Silksong',
    jaTenho: false,
    precosAtuais: [],
    historicoPrecos: [
      { id: 1, canalId: 1, canalNome: 'STEAM', precoAtual: 49, precoCheio: 99, disponivel: true, consultadoEm: '2026-09-01T10:00:00Z' },
      { id: 3, canalId: 2, canalNome: 'GOG', precoAtual: 39, precoCheio: 99, disponivel: true, consultadoEm: '2026-10-15T10:00:00Z' },
      { id: 2, canalId: 1, canalNome: 'STEAM', precoAtual: 19, precoCheio: 99, disponivel: true, consultadoEm: '2026-11-01T10:00:00Z' }
    ]
  }] });

  render(<WishlistScreen />);
  fireEvent.click(await screen.findByRole('button', { name: 'Ver histórico (3)' }));

  const steam = screen.getByRole('region', { name: 'Histórico STEAM' });
  const gog = screen.getByRole('region', { name: 'Histórico GOG' });
  expect(within(steam).getAllByLabelText('Preço atual').map((elemento) => elemento.textContent)).toEqual(['R$ 19,00', 'R$ 49,00']);
  expect(within(gog).getAllByLabelText('Preço atual').map((elemento) => elemento.textContent)).toEqual(['R$ 39,00']);
  expect(Array.from(steam.querySelectorAll('time')).map((elemento) => elemento.getAttribute('dateTime'))).toEqual([
    '2026-11-01T10:00:00Z',
    '2026-09-01T10:00:00Z'
  ]);
});
