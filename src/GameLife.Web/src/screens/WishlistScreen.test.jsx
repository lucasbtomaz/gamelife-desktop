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

  fetch.mockResolvedValueOnce({
    ok: true,
    json: async () => ({ id: 1, jogoId: 10, titulo: 'Hades', jaTenho: false, precosAtuais: [], historicoPrecos: [], assinaturas: [] })
  });

  fireEvent.change(screen.getByLabelText('Nome do jogo desejado'), { target: { value: 'Hades' } });
  fireEvent.click(screen.getByRole('button', { name: 'Adicionar desejo' }));

  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/lista-desejos', expect.objectContaining({ method: 'POST' })));
  expect(await screen.findByRole('heading', { name: 'Hades' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Adicionar assinatura para Hades' })).toBeInTheDocument();
  expect(screen.queryByLabelText('Serviço')).not.toBeInTheDocument();
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
